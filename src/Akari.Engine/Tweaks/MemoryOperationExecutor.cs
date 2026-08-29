// MemoryOperationExecutor — ITweakExecutor implementation for memory-based tweaks.
//
// Dispatches apply/revert for TweakType.Memory. Handles MEM-01 (memory compression toggle).
//
// Apply logic:
//   1. Execute PowerShellCommand via IMemoryManager.DisableCompressionAsync()
//   2. Log operation to ILogService (T-04-02 mitigation)
//
// Revert logic:
//   1. Execute PowerShellRevertCommand via IMemoryManager.EnableCompressionAsync()
//   2. Log operation to ILogService
//
// All operations are async Task with Task.Run offloading (D-11, Pitfall 5).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Memory;
using Akari.Engine.Storage;

namespace Akari.Engine.Tweaks;

/// <summary>
/// Executor for <see cref="TweakType.Memory"/> tweaks. Manages Windows memory
/// compression toggling via <see cref="IMemoryManager"/> (PowerShell MMAgent cmdlets).
/// All operations are async Task with Task.Run offloading (D-11, Pitfall 5).
/// </summary>
public class MemoryOperationExecutor : ITweakExecutor
{
    private readonly IMemoryManager _memoryManager;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public MemoryOperationExecutor(
        IMemoryManager memoryManager,
        ITweakStateService stateService,
        ILogService logService)
    {
        _memoryManager = memoryManager;
        _stateService = stateService;
        _logService = logService;
    }

    /// <inheritdoc/>
    public bool CanHandle(TweakType type) => type == TweakType.Memory;

    /// <inheritdoc/>
    public async Task<TweakResult> ApplyAsync(TweakDefinition definition)
    {
        return await Task.Run(async () =>
        {
            try
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Applying memory tweak: {definition.Id} ({definition.Name})");

                if (string.IsNullOrEmpty(definition.PowerShellCommand))
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing PowerShellCommand"
                    };
                }

                // Log the command being executed
                await _logService.LogAsync(LogLevel.Info,
                    $"Executing: {definition.PowerShellCommand}");

                // Check current state before applying
                var wasEnabled = await _memoryManager.IsCompressionEnabledAsync();
                await _logService.LogAsync(LogLevel.Info,
                    $"Memory compression currently: {(wasEnabled ? "enabled" : "disabled")}");

                // Execute the disable command
                var result = await _memoryManager.DisableCompressionAsync();

                if (result.Success)
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Memory compression disabled successfully: {result.Output}");

                    await _stateService.UpdateAsync(definition.Id, TweakStatus.Applied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = true,
                        Status = TweakStatus.Applied,
                        ActualValue = "MemoryCompression disabled"
                    };
                }

                await _logService.LogErrorAsync(
                    $"Failed to disable memory compression: {result.ErrorMessage}",
                    new InvalidOperationException(result.ErrorMessage ?? "Unknown error"));

                await _stateService.UpdateAsync(definition.Id, TweakStatus.NotApplied);

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = result.ErrorMessage ?? "Unknown error during memory compression disable"
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to apply memory tweak {definition.Id}: {ex.Message}", ex);
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
                    $"Reverting memory tweak: {definition.Id} ({definition.Name})");

                if (string.IsNullOrEmpty(definition.PowerShellRevertCommand))
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing PowerShellRevertCommand"
                    };
                }

                // Log the revert command being executed
                await _logService.LogAsync(LogLevel.Info,
                    $"Executing revert: {definition.PowerShellRevertCommand}");

                // Execute the enable command
                var result = await _memoryManager.EnableCompressionAsync();

                if (result.Success)
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Memory compression re-enabled: {result.Output}");

                    await _stateService.UpdateAsync(definition.Id, TweakStatus.NotApplied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = true,
                        Status = TweakStatus.NotApplied,
                        ActualValue = "MemoryCompression re-enabled (may require reboot)"
                    };
                }

                await _logService.LogErrorAsync(
                    $"Failed to re-enable memory compression: {result.ErrorMessage}",
                    new InvalidOperationException(result.ErrorMessage ?? "Unknown error"));

                await _stateService.UpdateAsync(definition.Id, TweakStatus.Applied);

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = false,
                    Status = TweakStatus.Applied,
                    ErrorMessage = result.ErrorMessage ?? "Unknown error during memory compression enable"
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to revert memory tweak {definition.Id}: {ex.Message}", ex);
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
}