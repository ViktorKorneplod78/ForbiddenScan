using System.Text;
using static System.Net.WebUtility;
namespace ForbiddenScan;
public static class ReportWriter
{
    public static void Write(string path, BenchmarkResult report)
    {
        var html = new StringBuilder("""
<!DOCTYPE html><html lang="ru"><meta charset="utf-8"><title>ForbiddenScan</title>
<style>body{font-family:Arial,sans-serif;margin:20px}table{border-collapse:collapse}th,td{border:1px solid #999;padding:3px 6px}</style>
<h1>ForbiddenScan</h1>
""");
        html.Append($"<p>{report.Created:yyyy-MM-dd HH:mm:ss}</p><table>");
        foreach (var (key, value) in report.Environment)
            html.Append($"<tr><th>{HtmlEncode(key)}</th><td>{HtmlEncode(value)}</td></tr>");
        html.Append($"</table><p>Шаблонов: {report.PatternCount}. Повторов: {report.Iterations}, после одного прогрева каждого алгоритма. Общая подготовка: {report.InputPreparationMs:F3} мс.</p>");
        html.Append("<p>Обработка включает чтение, разбор блоков, нормализацию и поиск. Запись логов исключена. Файловый кеш прогрет. Подготовка алгоритма измерена один раз, включая возможные затраты JIT.</p>");
        html.Append("<table><tr><th>Алгоритм</th><th>Подготовка, мс</th><th>Обработка, мс<br>мин / медиана / макс</th><th>Проверено блоков</th><th>Найдено блоков</th></tr>");
        foreach (var r in report.Algorithms)
            html.Append($"<tr><td>{HtmlEncode(r.Name)}</td><td>{r.PreparationMs:F3}</td><td>{r.TimesMs.Min():F3} / {r.Median:F3} / {r.TimesMs.Max():F3}</td><td>{r.Scan.Checked}</td><td>{r.Scan.Matches.Count}</td></tr>");
        html.Append("</table><p>Результаты алгоритмов совпадают.</p><ul>");
        foreach (string file in report.Files) html.Append($"<li>{HtmlEncode(file)}</li>");
        html.Append("</ul><p><a href=\"run.log\">Журнал</a> | <a href=\"matches.log\">Найденные блоки</a></p></html>");
        File.WriteAllText(path, html.ToString(), new UTF8Encoding(false));
    }
}
