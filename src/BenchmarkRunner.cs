using System.Diagnostics;
namespace ForbiddenScan;
public static class BenchmarkRunner
{
    public static readonly string[] Names = ["naive", "hash", "aho"];
    public static IBlockMatcher Create(string name, string[] patterns) => name switch
    {
        "naive" => new NaiveMatcher(patterns),
        "hash" => new HashMatcher(patterns),
        "aho" => new AhoCorasickMatcher(patterns),
        _ => throw new ArgumentException("Algorithm must be naive, hash or aho.")
    };
    public static void Run(BenchmarkResult report, string[] patterns, Logger log)
    {
        var matchers = new List<IBlockMatcher>();
        foreach (string name in Names)
        {
            var timer = Stopwatch.StartNew();
            var matcher = Create(name, patterns);
            timer.Stop();
            var result = new AlgorithmResult { Name = name, PreparationMs = timer.Elapsed.TotalMilliseconds };
            report.Algorithms.Add(result); matchers.Add(matcher);
            log.Write($"{name}: preparation {result.PreparationMs:F3} ms");
            result.Scan = ScanRunner.Run(report.Files, matcher);
            log.Write($"{name}: warm-up complete");
        }
        for (int run = 0; run < report.Iterations; run++)
        for (int index = 0; index < Names.Length; index++)
        {
            int selected = (index + run) % Names.Length;
            var result = report.Algorithms[selected];
            var timer = Stopwatch.StartNew();
            var scan = ScanRunner.Run(report.Files, matchers[selected]);
            timer.Stop();
            EnsureSame(report.Algorithms[0].Scan, scan);
            result.TimesMs.Add(timer.Elapsed.TotalMilliseconds);
            log.Write($"{result.Name}: iteration {run + 1}, processing {timer.Elapsed.TotalMilliseconds:F3} ms");
        }
        foreach (var result in report.Algorithms) EnsureSame(report.Algorithms[0].Scan, result.Scan);
    }
    public static void EnsureSame(ScanResult expected, ScanResult actual)
    {
        if (expected.Checked != actual.Checked || !expected.Matches.SequenceEqual(actual.Matches))
            throw new InvalidOperationException("Algorithms returned different blocks.");
    }
}
