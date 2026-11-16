namespace ForbiddenScan;
public record BlockId(string File, int Number);
public class ScanResult
{
    public int Checked { get; set; }
    public List<BlockId> Matches { get; set; } = new();
    public Dictionary<string, int> Counts { get; set; } = new();
}
