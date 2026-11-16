namespace ForbiddenScan;
// Последовательное чтение: в памяти только текущий блок.
public static class BlockReader
{
    public static IEnumerable<DocumentBlock> Stream(string path)
    {
        using var reader = new StreamReader(path, new System.Text.UTF8Encoding(false, true));
        foreach (var block in ReadLines(reader, Path.GetFileName(path))) yield return block;
    }
    public static IEnumerable<DocumentBlock> ReadLines(TextReader reader, string file)
    {
        var lines = new List<string>();
        int number = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            else if (lines.Count > 0) { yield return new(file, ++number, lines.ToArray()); lines.Clear(); }
        }
        // Последний блок может не иметь завершающей пустой строки.
        if (lines.Count > 0) yield return new(file, ++number, lines.ToArray());
    }
}
