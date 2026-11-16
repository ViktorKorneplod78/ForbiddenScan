using System.Diagnostics;
namespace ForbiddenScan;
public static class BenchmarkRunner
{
    public static readonly string[] Names = ["naive", "hash", "aho", "hash-aho"];
    public static IBlockMatcher Create(string name, string[] patterns) => name switch
    {
        "naive" => new NaiveMatcher(patterns),
        "hash" => new HashMatcher(patterns),
        "aho" => new AhoCorasickMatcher(patterns),
        "hash-aho" => new HashMatcher(patterns, p => new AhoCorasickMatcher(p)),
        _ => throw new ArgumentException("Algorithm must be naive, hash, aho or hash-aho.")
    };
    public static void Run(BenchmarkResult report, string[] patterns, Logger log)
    {
        // Прогреваем создание всех вариантов до замеров подготовки.
        report.PreparationWarmups = 5;
        for (int round = 0; round < report.PreparationWarmups; round++)
        foreach (string name in Names)
            GC.KeepAlive(Create(name, patterns));
        log.Write($"Preparation warm-up: {report.PreparationWarmups} creations per algorithm");

        // Каждый замер подготовки создает новый экземпляр; сохраняем последние для поиска.
        var matchers = new IBlockMatcher[Names.Length];
        foreach (string name in Names) report.Algorithms.Add(new AlgorithmResult { Name = name });
        for (int run = 0; run < report.Iterations; run++)
        for (int index = 0; index < Names.Length; index++)
        {
            // Меняем стартовый алгоритм между повторами.
            int selected = (index + run) % Names.Length;
            var result = report.Algorithms[selected];
            var timer = Stopwatch.StartNew();
            var matcher = Create(result.Name, patterns);
            timer.Stop();
            matchers[selected] = matcher;
            result.PreparationTimesMs.Add(timer.Elapsed.TotalMilliseconds);
            log.Write($"{result.Name}: preparation #{run + 1}, {timer.Elapsed.TotalMilliseconds:F6} ms");
        }
        foreach (var result in report.Algorithms)
        {
            var sorted = result.PreparationTimesMs.Order().ToArray();
            result.PreparationMs = sorted[sorted.Length / 2] / 2 + sorted[(sorted.Length - 1) / 2] / 2;
            log.Write($"{result.Name}: preparation median {result.PreparationMs:F6} ms");
        }
        for (int i = 0; i < Names.Length; i++)
        {
            report.Algorithms[i].Scan = ScanRunner.Run(report.Files, matchers[i]);
            log.Write($"{Names[i]}: processing warm-up complete");
        }

        for (int run = 0; run < report.Iterations; run++)
        for (int index = 0; index < Names.Length; index++)
        {
            // Меняем стартовый алгоритм между повторами.
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
    // Сверяем конкретные блоки, а не только число совпадений.
    public static void EnsureSame(ScanResult expected, ScanResult actual)
    {
        if (expected.Checked != actual.Checked || !expected.Matches.SequenceEqual(actual.Matches))
            throw new InvalidOperationException("Algorithms returned different blocks.");
    }
}
