// ProcessOperationExecutor — ITweakExecutor implementation for process-based tweaks.
//
// Dispatches apply/revert for TweakType.Process. Handles two sub-types:
//   - PROC-01 (process priority): Sets the priority class of a running process to High
//   - PROC-02 (background process management): Kills known game launcher processes
//
// Uses IProcessManager abstraction (D-09, D-11) for testability. All operations are
// async Task with Task.Run offloading (Pitfall 5). All operations logged via ILogService.

using System.Diagnostics;
using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Processes;
using Akari.Engine.Storage;

namespace Akari.Engine.Tweaks;

/// <summary>
/// Executor for <see cref="TweakType.Process"/> tweaks. Manages process priority
/// (PROC-01) and background process killing (PROC-02) via <see cref="IProcessManager"/>.
/// All operations are async Task with Task.Run offloading (D-11, Pitfall 5).
/// </summary>
public class ProcessOperationExecutor : ITweakExecutor
{
    private readonly IProcessManager _processManager;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public ProcessOperationExecutor(
        IProcessManager processManager,
        ITweakStateService stateService,
        ILogService logService)
    {
        _processManager = processManager;
        _stateService = stateService;
        _logService = logService;
    }

    /// <inheritdoc/>
    public bool CanHandle(TweakType type) => type == TweakType.Process;

    /// <inheritdoc/>
    public async Task<TweakResult> ApplyAsync(TweakDefinition definition)
    {
        return await Task.Run(async () =>
        {
            try
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Applying process tweak: {definition.Id} ({definition.Name})");

                if (definition.ProcessNames == null || definition.ProcessNames.Count == 0)
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing ProcessNames"
                    };
                }

                // Determine operation type based on definition
                if (!string.IsNullOrEmpty(definition.ProcessPriority))
                {
                    // PROC-01: Set process priority
                    var priority = ParsePriority(definition.ProcessPriority);
                    var results = new List<bool>();
                    foreach (var processName in definition.ProcessNames)
                    {
                        var success = await _processManager.SetPriorityAsync(processName, priority);
                        results.Add(success);

                        await _logService.LogAsync(success ? LogLevel.Info : LogLevel.Warning,
                            $"Process {processName}: priority set to {priority} (success={success})");
                    }

                    var allSuccess = results.All(r => r);
                    await _stateService.UpdateAsync(definition.Id,
                        allSuccess ? TweakStatus.Applied : TweakStatus.NotApplied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = allSuccess,
                        Status = allSuccess ? TweakStatus.Applied : TweakStatus.NotApplied,
                        ErrorMessage = allSuccess ? null : $"Failed to set priority for {results.Count(r => !r)} process(es)"
                    };
                }
                else
                {
                    // PROC-02: Kill background processes
                    await _logService.LogAsync(LogLevel.Info,
                        $"Killing background processes: {string.Join(", ", definition.ProcessNames)}");

                    var results = await _processManager.KillAsync(definition.ProcessNames);
                    var killedCount = results.Count(r => r);

                    await _logService.LogAsync(LogLevel.Info,
                        $"{killedCount}/{definition.ProcessNames.Count} background processes killed");

                    var success = killedCount > 0; // At least one process killed (some may not be running)
                    await _stateService.UpdateAsync(definition.Id,
                        success ? TweakStatus.Applied : TweakStatus.NotApplied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = success,
                        Status = success ? TweakStatus.Applied : TweakStatus.NotApplied,
                        ErrorMessage = success ? null : "No target processes were running"
                    };
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to apply process tweak {definition.Id}: {ex.Message}", ex);
                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<TweakResult> RevertAsync(TweakDefinition definition)
    {
        return await Task.Run(async () =>
        {
            try
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Reverting process tweak: {definition.Id} ({definition.Name})");

                // For PROC-01 (priority): revert means restoring to Normal priority
                if (!string.IsNullOrEmpty(definition.ProcessPriority))
                {
                    var results = new List<bool>();
                    foreach (var processName in definition.ProcessNames ?? Enumerable.Empty<string>())
                    {
                        var success = await _processManager.SetPriorityAsync(processName, ProcessPriorityClass.Normal);
                        results.Add(success);

                        await _logService.LogAsync(LogLevel.Info,
                            $"Process {processName}: priority restored to Normal (success={success})");
                    }

                    var allSuccess = results.All(r => r);
                    await _stateService.UpdateAsync(definition.Id,
                        allSuccess ? TweakStatus.NotApplied : TweakStatus.Applied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = allSuccess,
                        Status = allSuccess ? TweakStatus.NotApplied : TweakStatus.Applied,
                        ErrorMessage = allSuccess ? null : "Could not restore priority for some processes"
                    };
                }
                else
                {
                    // For PROC-02 (kill): revert means the processes are no longer killed
                    // (they would need to be restarted by the user/launcher)
                    await _logService.LogAsync(LogLevel.Info,
                        $"Background process tweak {definition.Id}: revert means processes" +
                        " are allowed to run again (user should restart launchers manually)");

                    await _stateService.UpdateAsync(definition.Id, TweakStatus.NotApplied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = true,
                        Status = TweakStatus.NotApplied,
                        ActualValue = "Processes no longer killed — restart launchers manually"
                    };
                }
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to revert process tweak {definition.Id}: {ex.Message}", ex);
                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = false,
                    Status = TweakStatus.Applied,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <summary>
    /// Parses a string priority name to ProcessPriorityClass enum.
    /// </summary>
    private static ProcessPriorityClass ParsePriority(string priorityName)
    {
        return Enum.TryParse<ProcessPriorityClass>(priorityName, true, out var priority)
            ? priority
            : ProcessPriorityClass.High;
    }
}