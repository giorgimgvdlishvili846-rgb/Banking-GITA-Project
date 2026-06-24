namespace Banking.Logging;

public class FileLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        string directory = Path.GetDirectoryName(_logFilePath)!;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warning(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_logFilePath, entry + Environment.NewLine);
            }
        }
        catch
        {
            // ლოგირების შეცდომა არ უნდა გააჩეროს აპლიკაცია
        }
    }
}
