// ProcessManager — production implementation wrapping System.Diagnostics.Process.
//
// Uses System.Diagnostics.Process for runtime process management. All operations are
// async Task with Task.Run offloading (D-11, Pitfall 5) to prevent UI thread blocking.
// This is the real implementation; FakeProcessManager is used for unit tests.

using System.Diagnostics;

namespace Akari.Engine.Processes;

/// <summary>
/// Production implementation of <see cref="IProcessManager"/> that wraps
/// <see cref="System.Diagnostics.Process"/> for real Windows process management.
/// All operations are async Task with Task.Run offloading to prevent UI blocking (D-11).
/// </summary>
public class ProcessManager : IProcessManager
{
    /// <inheritdoc/>
    public async Task<IEnumerable<IProcessInfo>> GetProcessesAsync(long minWorkingSetBytes = 0)
    {
        return await Task.Run(() =>
        {
            var processes = Process.GetProcesses();
            var result = new List<IProcessInfo>();

            foreach (var p in processes)
            {
                try
                {
                    var workingSet = p.WorkingSet64;
                    if (workingSet < minWorkingSetBytes) continue;

                    result.Add(new ProductionProcessInfo(
                        p.ProcessName,
                        p.Id,
                        workingSet,
                        p.PriorityClass));
                }
                catch
                {
                    // Process may have exited between enumeration and access — skip
                }
            }

            return (IEnumerable<IProcessInfo>)result;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> SetPriorityAsync(string processName, ProcessPriorityClass priority)
    {
        return await Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0) return false;

                foreach (var p in processes)
                {
                    try
                    {
                        p.PriorityClass = priority;
                    }
                    catch
                    {
                        // Process may have exited — ignore
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> KillAsync(string processName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0) return false;

                foreach (var p in processes)
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                        // Process may have exited — ignore
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<bool>> KillAsync(IEnumerable<string> processNames)
    {
        var names = processNames.ToList();
        var results = new List<bool>();

        foreach (var name in names)
        {
            results.Add(await KillAsync(name));
        }

        return results;
    }
}

/// <summary>
/// Production implementation of <see cref="IProcessInfo"/> wrapping a Process snapshot.
/// </summary>
internal class ProductionProcessInfo : IProcessInfo
{
    public string ProcessName { get; }
    public int Id { get; }
    public long WorkingSet64 { get; }
    public ProcessPriorityClass Priority { get; }

    public ProductionProcessInfo(string processName, int id, long workingSet64, ProcessPriorityClass priority)
    {
        ProcessName = processName;
        Id = id;
        WorkingSet64 = workingSet64;
        Priority = priority;
    }
}