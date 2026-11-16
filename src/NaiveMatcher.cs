namespace ForbiddenScan;
public class NaiveMatcher(string[] patterns) : IBlockMatcher
{
    public bool Matches(string text)
    {
        foreach (string pattern in patterns)
        {
            int offset = 0;
            while (offset <= text.Length - pattern.Length)
            {
                int at = text.IndexOf(pattern, offset, StringComparison.Ordinal);
                if (at < 0) break;
                if (TextNormalizer.Boundaries(text, at, pattern.Length)) return true;
                offset = at + 1;
            }
        }
        return false;
    }
}
