namespace ForbiddenScan;
// Общие правила регистра и границ для всех алгоритмов.
public static class TextNormalizer
{
    public static string Normalize(string text) => text.ToUpperInvariant();
    // Кавычки и дефисы считаются разделителями.
    public static bool Word(char c) => char.IsLetterOrDigit(c) || c == '_';
    public static bool Boundaries(string text, int start, int length) =>
        (start == 0 || !Word(text[start - 1])) &&
        (start + length == text.Length || !Word(text[start + length]));
    public static IEnumerable<string> Words(string text)
    {
        int start = 0;
        while (start < text.Length)
        {
            if (!Word(text[start])) { start++; continue; }
            int end = start + 1;
            while (end < text.Length && Word(text[end])) end++;
            yield return text[start..end]; start = end;
        }
    }
}
