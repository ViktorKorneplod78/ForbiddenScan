namespace ForbiddenScan;
public static class MatchesWriter
{
    public static void Write(TextWriter writer, DocumentBlock block)
    {
        writer.WriteLine($"{block.File} | block {block.Number}");
        foreach (string line in block.Participants) writer.WriteLine(line);
        writer.WriteLine();
    }
}
