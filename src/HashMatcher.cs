namespace ForbiddenScan;
public class HashMatcher : IBlockMatcher
{
    readonly HashSet<string> words = new(StringComparer.Ordinal);
    readonly NaiveMatcher full;
    readonly NaiveMatcher withoutWords;
    public HashMatcher(string[] patterns)
    {
        full = new(patterns);
        var fallback = new List<string>();
        foreach (var pattern in patterns)
        {
            var tokens = TextNormalizer.Words(pattern).ToArray();
            if (tokens.Length == 0) fallback.Add(pattern);
            foreach (var token in tokens) words.Add(token);
        }
        withoutWords = new(fallback.ToArray());
    }
    public bool Matches(string text) =>
        TextNormalizer.Words(text).Any(words.Contains) ? full.Matches(text) : withoutWords.Matches(text);
}
