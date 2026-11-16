namespace ForbiddenScan;
// Общие слова отбирают строки для полной проверки выбранным алгоритмом.
public class HashMatcher : IBlockMatcher
{
    readonly HashSet<string> words = new(StringComparer.Ordinal);
    readonly IBlockMatcher full;
    readonly IBlockMatcher withoutWords;
    public HashMatcher(string[] patterns) : this(patterns, p => new NaiveMatcher(p)) { }
    public HashMatcher(string[] patterns, Func<string[], IBlockMatcher> createMatcher)
    {
        full = createMatcher(patterns);
        var fallback = new List<string>();
        foreach (var pattern in patterns)
        {
            var tokens = TextNormalizer.Words(pattern).ToArray();
            if (tokens.Length == 0) fallback.Add(pattern);
            foreach (var token in tokens) words.Add(token);
        }
        // Шаблоны без слов нельзя отбрасывать словесным фильтром.
        withoutWords = createMatcher(fallback.ToArray());
    }
    public bool Matches(string text) =>
        TextNormalizer.Words(text).Any(words.Contains) ? full.Matches(text) : withoutWords.Matches(text);
}
