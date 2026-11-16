namespace ForbiddenScan;
public sealed class Logger(string path) : IDisposable
{
    readonly StreamWriter writer = new(path, false, new System.Text.UTF8Encoding(false));
    public void Write(string message, bool console = false)
    {
        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
        writer.Flush();
        if (console) Console.WriteLine(message);
    }
    public void Dispose() => writer.Dispose();
}
