namespace ForbiddenScan;
public static class ScanRunner
{
    public static ScanResult Run(string[] paths, IBlockMatcher matcher, TextWriter? output = null)
    {
        var result = new ScanResult();
        foreach (var path in paths)
        {
            string name = Path.GetFileName(path);
            result.Counts[name] = 0;
            foreach (var block in BlockReader.Stream(path))
            {
                result.Checked++;
                foreach (string participant in block.Participants)
                {
                    if (!matcher.Matches(TextNormalizer.Normalize(participant))) continue;
                    result.Matches.Add(new(block.File, block.Number));
                    result.Counts[name]++;
                    if (output != null) MatchesWriter.Write(output, block);
                    break;
                }
            }
        }
        return result;
    }
}
