using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
namespace ForbiddenScan;
// Команды проверки, сравнения алгоритмов и пересоздания отчета.
public static class Program
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    // Сведения сохраняются при запуске замеров, report их только читает.
    public static Dictionary<string, string> CollectEnvironment()
    {
        string cpu = System.Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown";
        string ram = "—";
        if (OperatingSystem.IsWindows())
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            cpu = (key?.GetValue("ProcessorNameString") as string)?.Trim() ?? cpu;
            var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
            if (GlobalMemoryStatusEx(ref memory))
                ram = (memory.TotalPhysical / (1024d * 1024 * 1024)).ToString("F1") + " GB";
        }
        return new()
        {
            ["Computer"] = System.Environment.MachineName,
            ["OS"] = RuntimeInformation.OSDescription,
            [".NET"] = RuntimeInformation.FrameworkDescription,
            ["Architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["CPU"] = cpu, ["RAM"] = ram,
            ["Logical processors"] = System.Environment.ProcessorCount.ToString(),
            ["User"] = System.Environment.UserName
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length, Load;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile,
            TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);


    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        string? logPath = null;
        try
        {
            if (args.Length == 0) { Console.WriteLine("Commands: scan [--algorithm naive|hash|aho|hash-aho] (default: hash-aho), benchmark [--iterations 5], report --run-dir DIR, self-test. scan/benchmark: --input DIR; output: reports/<timestamp>"); return 0; }
            if (args[0] == "self-test") { SelfTests.Run(); return 0; }
            if (args[0] is not ("scan" or "benchmark" or "report")) throw new ArgumentException("Unknown command.");
            var options = new Dictionary<string, string>();
            for (int i = 1; i < args.Length; i += 2)
            {
                if (!args[i].StartsWith("--") || i + 1 == args.Length) throw new ArgumentException("Expected --option value.");
                string key = args[i][2..];
                if (key is not ("input" or "run-dir" or "algorithm" or "iterations") || !options.TryAdd(key, args[i + 1]))
                    throw new ArgumentException("Unknown or repeated option: " + key);
            }
            if (args[0] == "report")
            {
                string dir = options["run-dir"];
                var saved = JsonSerializer.Deserialize<BenchmarkResult>(File.ReadAllText(Path.Combine(dir, "results.json")))!;
                ReportWriter.Write(Path.Combine(dir, "report.html"), saved); return 0;
            }
            if (options.ContainsKey("run-dir")) throw new ArgumentException("--run-dir is only supported by report. scan/benchmark create reports/<timestamp> automatically.");
            string input = Path.GetFullPath(options.GetValueOrDefault("input", "InputTerr"));
            string runDir = Path.GetFullPath(Path.Combine("reports", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff", System.Globalization.CultureInfo.InvariantCulture)));
            Directory.CreateDirectory(runDir);
            logPath = Path.Combine(runDir, "run.log");
            using var log = new Logger(logPath);
            var timer = Stopwatch.StartNew();
            var patterns = File.ReadLines(Path.Combine(input, "Запрещенные организации.txt"), new UTF8Encoding(false, true))
                .Select(x => TextNormalizer.Normalize(x.Trim())).Where(x => x.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
            string[] files = ["Список документов ams.txt", "Список документов arb.txt", "Список документов r002.txt"];
            files = files.Select(x => Path.Combine(input, x)).ToArray();
            foreach (var file in files) if (!File.Exists(file)) throw new FileNotFoundException(file);
            timer.Stop();
            var report = new BenchmarkResult { Files = files, PatternCount = patterns.Length, InputPreparationMs = timer.Elapsed.TotalMilliseconds,
                Iterations = int.Parse(options.GetValueOrDefault("iterations", "5")),
                Environment = CollectEnvironment() };
            if (report.Iterations < 1) throw new ArgumentException("Iterations must be positive.");
            log.Write($"Command: {args[0]}; patterns: {patterns.Length}; common preparation: {report.InputPreparationMs:F3} ms");
            ScanResult scan;
            if (args[0] == "benchmark")
            {
                BenchmarkRunner.Run(report, patterns, log);
                scan = report.Algorithms[0].Scan;
                // Полные блоки записываем отдельным проходом вне замеров.
                using (var output = new StreamWriter(Path.Combine(runDir, "matches.log"), false, new UTF8Encoding(false)))
                    BenchmarkRunner.EnsureSame(scan, ScanRunner.Run(files, BenchmarkRunner.Create("aho", patterns), output));
                File.WriteAllText(Path.Combine(runDir, "results.json"), JsonSerializer.Serialize(report, Json));
                ReportWriter.Write(Path.Combine(runDir, "report.html"), report);
                foreach (var r in report.Algorithms) log.Write($"{r.Name}: median {r.Median:F3} ms; preparation median {r.PreparationMs:F6} ms", true);
            }
            else
            {
                string name = options.GetValueOrDefault("algorithm", "hash-aho");
                timer.Restart(); var matcher = BenchmarkRunner.Create(name, patterns); timer.Stop();
                log.Write($"{name}: preparation {timer.Elapsed.TotalMilliseconds:F3} ms");
                timer.Restart(); scan = ScanRunner.Run(files, matcher); timer.Stop();
                log.Write($"{name}: processing {timer.Elapsed.TotalMilliseconds:F3} ms", true);
                using var output = new StreamWriter(Path.Combine(runDir, "matches.log"), false, new UTF8Encoding(false));
                // Повторное чтение для лога не входит во время поиска.
                var found = scan.Matches.ToHashSet();
                foreach (string file in files)
                foreach (var block in BlockReader.Stream(file))
                    if (found.Contains(new(block.File, block.Number))) MatchesWriter.Write(output, block);
            }
            log.Write($"Checked blocks: {scan.Checked}; matched blocks: {scan.Matches.Count}", true);
            foreach (var (file, count) in scan.Counts) log.Write($"{file}: {count}", true);
            log.Write($"Output: {runDir}", true);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            if (logPath != null)
            {
                try { File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR: {ex.Message}{System.Environment.NewLine}"); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return 1;
        }
    }
}
