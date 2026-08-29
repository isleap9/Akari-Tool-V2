// PowerOperationExecutor — ITweakExecutor implementation for power-based tweaks.
//
// Dispatches apply/revert for TweakType.Power. Handles PWR-01 (Ultimate Performance)
// and PWR-02 (High Performance fallback).
//
// Apply logic:
//   1. If the scheme has a PowerBaseSchemeGuid (PWR-01), attempt to duplicate it
//   2. Attempt to activate the target PowerSchemeGuid
//   3. If activation fails (Pitfall 9 — GUID confusion), try the fallback scheme
//   4. Log all operations to ILogService (T-04-02 mitigation)
//
// Revert logic:
//   1. Restore default power schemes via powercfg -restoredefaultschemes
//   2. Re-enable hibernate
//   3. Log all operations
//
// All operations are async Task with Task.Run offloading (D-11, Pitfall 5).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Power;
using Akari.Engine.Storage;

namespace Akari.Engine.Tweaks;

/// <summary>
/// Executor for <see cref="TweakType.Power"/> tweaks. Manages Windows power plan
/// activation via <see cref="IPowerManager"/>. Implements fallback from Ultimate
/// Performance to High Performance when the scheme is not available (Pitfall 9).
/// All operations are async Task with Task.Run offloading (D-11, Pitfall 5).
/// </summary>
public class PowerOperationExecutor : ITweakExecutor
{
    private readonly IPowerManager _powerManager;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public PowerOperationExecutor(
        IPowerManager powerManager,
        ITweakStateService stateService,
        ILogService logService)
    {
        _powerManager = powerManager;
        _stateService = stateService;
        _logService = logService;
    }

    /// <inheritdoc/>
    public bool CanHandle(TweakType type) => type == TweakType.Power;

    /// <inheritdoc/>
    public async Task<TweakResult> ApplyAsync(TweakDefinition definition)
    {
        return await Task.Run(async () =>
        {
            try
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Applying power tweak: {definition.Id} ({definition.Name})");

                if (string.IsNullOrEmpty(definition.PowerSchemeGuid))
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing PowerSchemeGuid"
                    };
                }

                // Step 1: If base scheme GUID is provided (PWR-01), duplicate it
                // Per Pitfall 9: validate GUID before activation to avoid confusion
                if (!string.IsNullOrEmpty(definition.PowerBaseSchemeGuid))
                {
                    var schemes = await _powerManager.ListSchemesAsync();
                    var schemeList = schemes.ToList();
                    var baseExists = schemeList.Any(s => s.Guid == definition.PowerBaseSchemeGuid);

                    if (baseExists)
                    {
                        await _logService.LogAsync(LogLevel.Info,
                            $"Base scheme {definition.PowerBaseSchemeGuid} found — duplicating to target {definition.PowerSchemeGuid}");

                        var dupResult = await _powerManager.DuplicateSchemeAsync(
                            definition.PowerBaseSchemeGuid!, definition.PowerSchemeGuid!);

                        if (!dupResult.Success)
                        {
                            await _logService.LogAsync(LogLevel.Warning,
                                $"Failed to duplicate scheme: {dupResult.ErrorMessage}. Attempting direct activation.");
                        }
                        else
                        {
                            await _logService.LogAsync(LogLevel.Info,
                                $"Scheme duplicated successfully: {dupResult.Output}");
                        }
                    }
                    else
                    {
                        await _logService.LogAsync(LogLevel.Warning,
                            $"Base scheme {definition.PowerBaseSchemeGuid} not found in powercfg /LIST." +
                            " Proceeding with direct activation attempt.");
                    }
                }

                // Step 2: Attempt to activate the target scheme
                var activateResult = await _powerManager.SetActiveSchemeAsync(definition.PowerSchemeGuid);

                if (activateResult.Success)
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Power scheme {definition.PowerSchemeGuid} activated successfully");

                    await _stateService.UpdateAsync(definition.Id, TweakStatus.Applied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = true,
                        Status = TweakStatus.Applied,
                        ActualValue = $"Active: {definition.PowerSchemeGuid}"
                    };
                }

                // Step 3: Fallback to High Performance (PWR-02 fallback, Pitfall 9)
                await _logService.LogAsync(LogLevel.Warning,
                    $"Failed to activate {definition.PowerSchemeGuid}: {activateResult.ErrorMessage}." +
                    " Falling back to High Performance plan (PWR-02).");

                var fallbackGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
                var fallbackResult = await _powerManager.SetActiveSchemeAsync(fallbackGuid);

                if (fallbackResult.Success)
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Fallback to High Performance scheme succeeded");

                    await _stateService.UpdateAsync(definition.Id, TweakStatus.Applied);

                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = true,
                        Status = TweakStatus.Applied,
                        ActualValue = $"Active: {fallbackGuid} (fallback)"
                    };
                }

                await _logService.LogErrorAsync(
                    $"Both primary and fallback power scheme activation failed for {definition.Id}",
                    new InvalidOperationException($"Primary: {activateResult.ErrorMessage}; Fallback: {fallbackResult.ErrorMessage}"));

                await _stateService.UpdateAsync(definition.Id, TweakStatus.NotApplied);

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = $"Primary failed: {activateResult.ErrorMessage}; Fallback failed: {fallbackResult.ErrorMessage}"
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to apply power tweak {definition.Id}: {ex.Message}", ex);
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
                    $"Reverting power tweak: {definition.Id} ({definition.Name})");

                if (string.IsNullOrEmpty(definition.PowerSchemeGuid))
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing PowerSchemeGuid"
                    };
                }

                // Restore default power schemes
                var restoreResult = await _powerManager.RestoreDefaultSchemesAsync();
                if (restoreResult.Success)
                {
                    await _logService.LogAsync(LogLevel.Info,
                        "Default power schemes restored");
                }
                else
                {
                    await _logService.LogAsync(LogLevel.Warning,
                        $"Failed to restore default schemes: {restoreResult.ErrorMessage}");
                }

                // Re-enable hibernate (Power Plan.ps1 revert path, line 243)
                var hibernateResult = await _powerManager.SetHibernateAsync(true);
                if (hibernateResult.Success)
                {
                    await _logService.LogAsync(LogLevel.Info, "Hibernate enabled");
                }

                await _stateService.UpdateAsync(definition.Id, TweakStatus.NotApplied);

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = restoreResult.Success,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = restoreResult.Success ? null : restoreResult.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to revert power tweak {definition.Id}: {ex.Message}", ex);
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
}