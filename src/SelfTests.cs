namespace ForbiddenScan;
public static class SelfTests
{
    public static void Run()
    {
        (string[] patterns, string text, bool expected)[] cases =
        [
            (["АЗОВ"], "нет совпадений", false),
            (["АЗОВ"], "Объединение «Азов»", true),
            (["АЗОВ"], "Азовский", false),
            (["АЗОВ"], "Суперазов", false),
            (["АЗОВ"], "Азовский, затем Азов", true),
            (["ООО АЗОВ"], "Организация Азов", false),
            (["А Б"], "А и Б", false),
            (["А-Б"], "«а-б»", true),
            (["++"], "++", true),
            (["XАБ", "Б"], "XАБ Б", true),
            (["А Б", "Б"], "ЯА Б", true)
        ];
        foreach (var (patterns, text, expected) in cases)
        foreach (string name in BenchmarkRunner.Names)
            if (BenchmarkRunner.Create(name, patterns).Matches(TextNormalizer.Normalize(text)) != expected)
                throw new Exception($"Test failed: {name}: {text}");
        using var reader = new StringReader("\n \nfirst\nfirst\n\n\nlast");
        var blocks = BlockReader.ReadLines(reader, "test").ToArray();
        if (blocks.Length != 2 || blocks[1].Number != 2 || blocks[0].Participants.Length != 2)
            throw new Exception("Block reader test failed.");
        Console.WriteLine("Self-tests passed.");
    }
}
