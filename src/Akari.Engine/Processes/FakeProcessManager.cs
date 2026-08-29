// FakeProcessManager — in-memory implementation for unit tests.
//
// CRITICAL (D-04, PITFALLS.md Pitfall 2):
// This manager does NOT interact with real Windows processes. It is an in-memory
// dictionary with no actual process manipulation. Unit tests using this fake
// validate LOGIC ONLY. Runtime verification of actual process operations
// must be performed via an elevated app launch and log file inspection at
// %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.

using System.Diagnostics;

namespace Akari.Engine.Processes;

/// <summary>
/// In-memory implementation of <see cref="IProcessManager"/> for unit tests.
/// Does NOT interact with real Windows processes — logic-only validation
/// (per D-04, PITFALLS.md Pitfall 2). Runtime process operations require an
/// actual elevated launch + log check.
/// </summary>
public class FakeProcessManager : IProcessManager
{
    private readonly List<FakeProcessInfo> _processes = new();

    /// <summary>
    /// Registers a fake process for testing.
    /// </summary>
    public void RegisterProcess(string processName, int id = 0, long workingSet64 = 500_000_000,
        ProcessPriorityClass priority = ProcessPriorityClass.Normal)
    {
        _processes.Add(new FakeProcessInfo(processName, id == 0 ? _processes.Count + 1 : id,
            workingSet64, priority));
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IProcessInfo>> GetProcessesAsync(long minWorkingSetBytes = 0)
    {
        return Task.FromResult<IEnumerable<IProcessInfo>>(
            _processes.Where(p => p.WorkingSet64 >= minWorkingSetBytes).ToList());
    }

    /// <inheritdoc/>
    public Task<bool> SetPriorityAsync(string processName, ProcessPriorityClass priority)
    {
        var process = _processes.FirstOrDefault(p =>
            p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        if (process != null)
        {
            process.Priority = priority;
            process.SetPriorityCalled = true;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<bool> KillAsync(string processName)
    {
        var process = _processes.FirstOrDefault(p =>
            p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        if (process != null)
        {
            process.IsKilled = true;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<bool>> KillAsync(IEnumerable<string> processNames)
    {
        var results = new List<bool>();
        foreach (var name in processNames)
        {
            results.Add(await KillAsync(name));
        }
        return results;
    }

    /// <summary>
    /// Verifies that SetPriorityAsync was called for the given process with the given priority.
    /// </summary>
    public bool WasPrioritySet(string processName, ProcessPriorityClass priority)
    {
        var process = _processes.FirstOrDefault(p =>
            p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        return process?.SetPriorityCalled == true &&
               process?.Priority == priority;
    }

    /// <summary>
    /// Verifies that KillAsync was called for the given process.
    /// </summary>
    public bool WasProcessKilled(string processName)
    {
        var process = _processes.FirstOrDefault(p =>
            p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        return process?.IsKilled == true;
    }

    /// <summary>
    /// Returns the current priority class for a registered process as a string,
    /// or null if the process was not found.
    /// </summary>
    public string? GetPriority(string processName)
    {
        var process = _processes.FirstOrDefault(p =>
            p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        return process?.Priority.ToString();
    }

    /// <summary>
    /// Returns the count of processes that were killed.
    /// </summary>
    public int TotalKilled => _processes.Count(p => p.IsKilled);

    /// <summary>
    /// Returns the count of processes that had priority changed.
    /// </summary>
    public int TotalPrioritySet => _processes.Count(p => p.SetPriorityCalled);
}

/// <summary>
/// In-memory fake process info for testing.
/// </summary>
internal class FakeProcessInfo : IProcessInfo
{
    public string ProcessName { get; }
    public int Id { get; }
    public long WorkingSet64 { get; }
    public ProcessPriorityClass Priority { get; set; }

    // Test-only tracking fields
    internal bool SetPriorityCalled { get; set; }
    internal bool IsKilled { get; set; }

    public FakeProcessInfo(string processName, int id, long workingSet64, ProcessPriorityClass priority)
    {
        ProcessName = processName;
        Id = id;
        WorkingSet64 = workingSet64;
        Priority = priority;
    }
}
