namespace ForbiddenScan;
public class AlgorithmResult
{
    public string Name { get; set; } = "";
    public double PreparationMs { get; set; } // Медиана; в старых отчетах — однократный замер.
    public List<double> PreparationTimesMs { get; set; } = new();
    public List<double> TimesMs { get; set; } = new();
    public ScanResult Scan { get; set; } = new();
    public double Median => TimesMs.Order().ElementAt(TimesMs.Count / 2) / 2 +
        TimesMs.Order().ElementAt((TimesMs.Count - 1) / 2) / 2;
}
// Сохраненные данные позволяют пересоздать HTML без повторного эксперимента.
public class BenchmarkResult
{
    public DateTime Created { get; set; } = DateTime.Now;
    public Dictionary<string, string> Environment { get; set; } = new();
    public string[] Files { get; set; } = [];
    public int PatternCount { get; set; }
    public double InputPreparationMs { get; set; }
    public int Iterations { get; set; }
    public int PreparationWarmups { get; set; }
    public List<AlgorithmResult> Algorithms { get; set; } = new();
}
