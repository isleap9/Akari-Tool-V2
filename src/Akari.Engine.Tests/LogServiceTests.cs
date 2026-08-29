// LogServiceTests — tests FileLogService file creation and format.
//
// Per D-03: log file at %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log is the
// ground-truth feedback loop for runtime-only failures (FakeRegistryProvider tests
// cannot catch UnauthorizedAccessException — Pitfall 2, D-04).

using System.Text.RegularExpressions;
using Akari.Engine.Logging;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for FileLogService — verifies log file creation, content format,
/// and exception detail inclusion (D-03, PLAN.md task 3, success criteria #4).
/// </summary>
public class LogServiceTests
{
    /// <summary>
    /// Test: LogAsync creates the log directory and file on first write.
    /// </summary>
    [Fact]
    public async Task LogService_LogAsync_CreatesLogFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);

        // Act
        await logService.LogAsync(LogLevel.Info, "Test log message");

        // Assert
        Assert.True(File.Exists(logService.LogFilePath));
        var content = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Test log message", content);
        Assert.Contains("[Info]", content);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: Log format matches spec: {timestamp} [{level}] {message}.
    /// </summary>
    [Fact]
    public async Task LogService_LogAsync_WritesCorrectFormat()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);

        // Act
        await logService.LogAsync(LogLevel.Info, "Format test message");

        // Assert
        var content = await File.ReadAllTextAsync(logService.LogFilePath);

        // Format: {timestamp:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}
        var expectedPattern = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \[Info\] Format test message";
        Assert.Matches(expectedPattern, content.TrimEnd());

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: LogErrorAsync includes exception details (type, message, stack trace).
    /// </summary>
    [Fact]
    public async Task LogService_LogErrorAsync_IncludesExceptionDetails()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);
        var ex = new InvalidOperationException("Test exception message");

        // Act
        await logService.LogErrorAsync("Error occurred", ex);

        // Assert
        var content = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("[Error]", content);
        Assert.Contains("Error occurred", content);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("Test exception message", content);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: Log file uses YYYY-MM-DD date format in filename (per D-03).
    /// </summary>
    [Fact]
    public async Task LogService_LogFilePath_UsesDateInFilename()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);

        // Act
        await logService.LogAsync(LogLevel.Info, "Date format test");

        // Assert
        var filename = Path.GetFileName(logService.LogFilePath);
        // app-YYYY-MM-DD.log
        var datePattern = @"^app-\d{4}-\d{2}-\d{2}\.log$";
        Assert.Matches(datePattern, filename);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: Multiple log entries are appended to the same file.
    /// </summary>
    [Fact]
    public async Task LogService_MultipleLogs_AppendedToFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);

        // Act
        await logService.LogAsync(LogLevel.Info, "First message");
        await logService.LogAsync(LogLevel.Warning, "Second message");
        await logService.LogAsync(LogLevel.Error, "Third message");

        // Assert
        var content = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("First message", content);
        Assert.Contains("Second message", content);
        Assert.Contains("Third message", content);
        Assert.Contains("[Warning]", content);
        Assert.Contains("[Error]", content);

        Cleanup(tempDir);
    }

    private static void Cleanup(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }
}
