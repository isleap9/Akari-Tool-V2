// IProcessManager — abstracts System.Diagnostics.Process for testability.
//
// The production ProcessManager wraps System.Diagnostics.Process.
// The FakeProcessManager is an in-memory implementation for unit tests.
// This abstraction is necessary because Process manipulation requires
// actual running processes — cannot be tested in unit tests without it.

using System.Diagnostics;

namespace Akari.Engine.Processes;

/// <summary>
/// Represents a running Windows process that can have its priority adjusted
/// or be killed. This is the testable abstraction over
/// <see cref="System.Diagnostics.Process"/>.
/// </summary>
public interface IProcessInfo
{
    /// <summary>Process name (e.g. "game.exe").</summary>
    string ProcessName { get; }

    /// <summary>Process ID.</summary>
    int Id { get; }

    /// <summary>Memory usage in bytes.</summary>
    long WorkingSet64 { get; }

    /// <summary>Current priority class.</summary>
    ProcessPriorityClass Priority { get; }
}

/// <summary>
/// Factory interface for creating <see cref="IProcessInfo"/> instances and
/// managing process operations. Allows injecting a fake for unit testing.
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Returns all running processes, optionally filtered by minimum working set size.
    /// </summary>
    Task<IEnumerable<IProcessInfo>> GetProcessesAsync(long minWorkingSetBytes = 0);

    /// <summary>
    /// Sets the priority class for a process identified by name.
    /// Returns true if the priority was set successfully.
    /// </summary>
    Task<bool> SetPriorityAsync(string processName, ProcessPriorityClass priority);

    /// <summary>
    /// Kills a process by name. Returns true if killed successfully.
    /// </summary>
    Task<bool> KillAsync(string processName);

    /// <summary>
    /// Kills multiple processes by name. Returns a list of success flags in order.
    /// </summary>
    Task<IReadOnlyList<bool>> KillAsync(IEnumerable<string> processNames);
}