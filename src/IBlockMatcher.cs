namespace ForbiddenScan;
// Строка уже нормализована; обход блоков и ранний выход выполняет ScanRunner.
public interface IBlockMatcher { bool Matches(string participant); }
