namespace ForbiddenScan;
// Input is already normalized. The common scanner handles blocks and early exit.
public interface IBlockMatcher { bool Matches(string participant); }
