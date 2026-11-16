using System.Text;
using static System.Net.WebUtility;
namespace ForbiddenScan;
// Только условия, таблицы результатов и ссылки на журналы.
public static class ReportWriter
{
    private static string Preparation(AlgorithmResult r) => r.PreparationTimesMs.Count == 0
        ? r.PreparationMs.ToString("F4")
        : $"{r.PreparationTimesMs.Min():F4} / {r.PreparationMs:F4} / {r.PreparationTimesMs.Max():F4}";

    public static void Write(string path, BenchmarkResult report)
    {
        var html = new StringBuilder("""
<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="utf-8">
<title>Отчет о проверке организаций</title>
<style>
  body { font-family: Arial, sans-serif; margin: 20px; }
  table { border-collapse: collapse; }
  th, td { border: 1px solid #999; padding: 3px 6px; }
</style>
</head>
<body>
<h1>Отчет о проверке организаций</h1>
""");
        void Row(string name, string value) =>
            html.Append($"<tr><th>{HtmlEncode(name)}</th><td>{HtmlEncode(value)}</td></tr>");

        html.Append($"<p>Дата: {report.Created:dd.MM.yyyy HH:mm:ss}</p><h2>Условия тестирования</h2><table>");
        string Value(string key) => report.Environment.GetValueOrDefault(key, "—");
        Row("Компьютер", Value("Computer"));
        Row("ОС", $"{Value("OS")} ({Value("Architecture")})");
        Row("Процессор", Value("CPU"));
        Row("Логических ядер", Value("Logical processors"));
        Row("Оперативная память", Value("RAM"));
        Row(".NET", Value(".NET"));
        Row("Пользователь", Value("User"));
        Row("Замеров обработки на алгоритм", $"{report.Iterations} (+1 прогревочный)");
        Row("Замеров подготовки на алгоритм", report.PreparationWarmups > 0
            ? $"{report.Iterations} (+{report.PreparationWarmups} прогревочных)" : "1");
        Row("Запрещенных названий", report.PatternCount.ToString());
        Row("Общая подготовка, мс", report.InputPreparationMs.ToString("F3"));
        html.Append("</table><h3>Входные файлы</h3><table><tr><th>Файл</th><th>Найдено блоков</th></tr>");
        foreach (string file in report.Files)
        {
            string count = report.Algorithms.FirstOrDefault()?.Scan.Counts
                .GetValueOrDefault(Path.GetFileName(file)).ToString() ?? "—";
            html.Append($"<tr><td>{HtmlEncode(file)}</td><td>{count}</td></tr>");
        }
        html.Append("</table><h2>Результаты</h2><table><tr><th>Алгоритм</th><th>Подготовка, мс<br>мин / медиана / макс</th><th>Обработка, мс<br>мин / медиана / макс</th><th>Проверено блоков</th><th>Найдено блоков</th><th>Результат</th></tr>");
        var names = new Dictionary<string, string>
        {
            ["naive"] = "Перебор", ["hash"] = "HashSet + перебор",
            ["aho"] = "Ахо — Корасик", ["hash-aho"] = "HashSet + Ахо — Корасик"
        };
        foreach (var r in report.Algorithms)
            html.Append($"<tr><td>{HtmlEncode(names.GetValueOrDefault(r.Name, r.Name))}</td><td>{Preparation(r)}</td><td>{r.TimesMs.Min():F3} / {r.Median:F3} / {r.TimesMs.Max():F3}</td><td>{r.Scan.Checked}</td><td>{r.Scan.Matches.Count}</td><td>Совпадает с базовым</td></tr>");
        html.Append("</table><h2>Материалы прогона</h2><table>");
        html.Append("<tr><th>Лог выполнения</th><td><a href=\"run.log\">run.log</a></td></tr>");
        html.Append("<tr><th>Найденные блоки</th><td><a href=\"matches.log\">matches.log</a></td></tr>");
        html.Append("<tr><th>Результаты измерений</th><td><a href=\"results.json\">results.json</a></td></tr></table></body></html>");
        File.WriteAllText(path, html.ToString(), new UTF8Encoding(false));
    }
}
