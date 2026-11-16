namespace ForbiddenScan;
public class AhoCorasickMatcher : IBlockMatcher
{
    class Node
    {
        public Dictionary<char, int> Next = new();
        public int Fail;
        public List<int> Lengths = new();
    }
    readonly List<Node> nodes = new() { new() };
    public AhoCorasickMatcher(string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            int state = 0;
            foreach (char c in pattern)
            {
                if (!nodes[state].Next.TryGetValue(c, out int next))
                {
                    next = nodes.Count; nodes[state].Next[c] = next; nodes.Add(new());
                }
                state = next;
            }
            nodes[state].Lengths.Add(pattern.Length);
        }
        var queue = new Queue<int>(nodes[0].Next.Values);
        while (queue.TryDequeue(out int parent))
        {
            foreach (var (c, child) in nodes[parent].Next)
            {
                int fail = nodes[parent].Fail;
                while (fail != 0 && !nodes[fail].Next.ContainsKey(c)) fail = nodes[fail].Fail;
                if (nodes[fail].Next.TryGetValue(c, out int target)) nodes[child].Fail = target;
                nodes[child].Lengths.AddRange(nodes[nodes[child].Fail].Lengths);
                queue.Enqueue(child);
            }
        }
    }
    public bool Matches(string text)
    {
        int state = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            while (state != 0 && !nodes[state].Next.ContainsKey(c)) state = nodes[state].Fail;
            state = nodes[state].Next.GetValueOrDefault(c);
            foreach (int length in nodes[state].Lengths)
                if (TextNormalizer.Boundaries(text, i + 1 - length, length)) return true;
        }
        return false;
    }
}
