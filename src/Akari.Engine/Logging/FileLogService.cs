// FileLogService — writes log entries to %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.
//
// Log format: {timestamp:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{exception}
// (per D-03, PLAN.md task 3, success criteria #4).
//
// This log file is the ground-truth feedback loop for runtime-only failures:
// FakeRegistryProvider tests cannot catch UnauthorizedAccessException — only an
// elevated app launch + log inspection can surface that (Pitfall 2, D-04).

using System.Text;

namespace Akari.Engine.Logging;

/// <summary>
/// File-based implementation of <see cref="ILogService"/>.
/// Writes to <c>%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log</c> (per D-03).
/// </summary>
public class FileLogService : ILogService
{
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new FileLogService. Creates the log directory if it does not exist.
    /// </summary>
    public FileLogService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(localAppData, "Akari", "App", "logs");
        _logFilePath = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
    }

    /// <summary>
    /// Initializes a new FileLogService with a custom log directory (for testing).
    /// </summary>
    public FileLogService(string customLogDirectory)
    {
        _logDirectory = customLogDirectory;
        _logFilePath = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyy-MM-dd}.log");
    }

    /// <summary>
    /// Gets the full path to the current log file (for verification/testing).
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <inheritdoc/>
    public async Task LogAsync(LogLevel level, string message, Exception? ex = null)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_logDirectory);

                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.Append($" [{level}] ");
                sb.Append(message);

                if (ex != null)
                {
                    sb.Append($"\n  Exception: {ex.GetType().FullName}: {ex.Message}");
                    sb.Append($"\n  StackTrace: {ex.StackTrace}");
                }

                sb.Append(Environment.NewLine);

                File.AppendAllText(_logFilePath, sb.ToString(), Encoding.UTF8);
            }
        });
    }

    /// <inheritdoc/>
    public async Task LogErrorAsync(string message, Exception? ex = null)
    {
        await LogAsync(LogLevel.Error, message, ex);
    }
}
