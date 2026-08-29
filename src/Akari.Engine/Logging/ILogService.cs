// ILogService — application-wide logging interface.
//
// Writes to %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log (per D-03).
// This log file is the ground-truth feedback loop for runtime-only failures
// (Pitfall 2: FakeRegistryProvider tests cannot catch UnauthorizedAccessException).

namespace Akari.Engine.Logging;

/// <summary>
/// Log level for categorizing log entries.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

/// <summary>
/// Application logging service. Writes to
/// <c>%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log</c> (per D-03).
/// This file is the ground-truth for runtime ACL failures (Pitfall 2, D-03).
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Asynchronously writes a log entry with the specified level and message.
    /// All operations are async Task with Task.Run offloading (ENG-04).
    /// </summary>
    Task LogAsync(LogLevel level, string message, Exception? ex = null);

    /// <summary>
    /// Asynchronously writes an error-level log entry with exception details.
    /// </summary>
    Task LogErrorAsync(string message, Exception? ex = null);
}
