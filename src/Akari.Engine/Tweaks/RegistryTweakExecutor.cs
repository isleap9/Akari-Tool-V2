// RegistryTweakExecutor — ITweakExecutor implementation for registry-based tweaks.
//
// Uses IRegistryProvider (from Plan 01-01, 2-arg OpenSubKey, RegistryView.Registry64)
// for actual registry operations. Never bypasses the abstraction — all writes go
// through IRegistryProvider.GetValueAsync/SetValueAsync (T-03-SC mitigation).
//
// Uses ITweakStateService (from Plan 01-02) for state tracking after each operation.
// Uses ILogService for all operation logging (T-02-02 mitigation).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks;

/// <summary>
/// Executor for <see cref="TweakType.Registry"/> tweaks. Dispatches apply/revert
/// to the <see cref="IRegistryProvider"/> abstraction (2-arg OpenSubKey, RegistryView.Registry64).
/// Uses <see cref="ITweakStateService"/> for state tracking after each operation (ENG-06).
/// </summary>
public class RegistryTweakExecutor : ITweakExecutor
{
    private readonly IRegistryProvider _registry;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public RegistryTweakExecutor(
        IRegistryProvider registry,
        ITweakStateService stateService,
        ILogService logService)
    {
        _registry = registry;
        _stateService = stateService;
        _logService = logService;
    }

    /// <inheritdoc/>
    public bool CanHandle(TweakType type) => type == TweakType.Registry;

    /// <inheritdoc/>
    public async Task<TweakResult> ApplyAsync(TweakDefinition definition)
    {
        return await Task.Run(async () =>
        {
            try
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Applying registry tweak: {definition.Id} ({definition.Name})");

                if (definition.RegistryKey == null || definition.RegistryValueName == null)
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing RegistryKey or RegistryValueName"
                    };
                }

                var kind = definition.RegistryValueKind ?? RegistryValueKind.DWord;
                var valueData = ParseValueData(definition.RegistryValueData, kind);

                // For multi-value tweaks (Mouse Acceleration), apply all values
                await ApplyRegistryValuesAsync(definition, valueData!, kind);

                await _stateService.UpdateAsync(definition.Id, TweakStatus.Applied);

                await _logService.LogAsync(LogLevel.Info,
                    $"Successfully applied tweak: {definition.Id}");

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = true,
                    Status = TweakStatus.Applied,
                    ActualValue = definition.RegistryValueData
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to apply tweak {definition.Id}: {ex.Message}", ex);

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
                    $"Reverting registry tweak: {definition.Id} ({definition.Name})");

                if (definition.RegistryKey == null || definition.RegistryValueName == null)
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing RegistryKey or RegistryValueName"
                    };
                }

                var kind = definition.RegistryValueKind ?? RegistryValueKind.DWord;
                var revertValueData = definition.RegistryRevertValueData;

                // For single-value toggles, write the disabled/default value
                if (!string.IsNullOrEmpty(revertValueData))
                {
                    var value = ParseValueData(revertValueData, kind);
                    await _registry.SetValueAsync(
                        definition.RegistryKey,
                        definition.RegistryValueName,
                        value!,
                        kind);
                }
                else
                {
                    // Delete the value to restore default state
                    await _registry.DeleteValueAsync(
                        definition.RegistryKey,
                        definition.RegistryValueName);
                }

                await _stateService.UpdateAsync(definition.Id, TweakStatus.NotApplied);

                await _logService.LogAsync(LogLevel.Info,
                    $"Successfully reverted tweak: {definition.Id}");

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = true,
                    Status = TweakStatus.NotApplied,
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to revert tweak {definition.Id}: {ex.Message}", ex);

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = false,
                    Status = TweakStatus.Applied, // Keep as Applied since revert failed
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <summary>
    /// Applies registry values for the tweak. Handles both single-value and
    /// multi-value (Mouse Acceleration) tweaks.
    /// </summary>
    private async Task ApplyRegistryValuesAsync(TweakDefinition definition, object? valueData, RegistryValueKind kind)
    {
        if (definition.RegistryMultiValues == null || definition.RegistryMultiValues.Count == 0)
        {
            // Single value tweak
            await _registry.SetValueAsync(
                definition.RegistryKey!,
                definition.RegistryValueName!,
                valueData!,
                kind);
        }
        else
        {
            // Multi-value tweak (e.g. Mouse Acceleration: MouseSpeed, MouseThreshold1, MouseThreshold2)
            foreach (var multiValue in definition.RegistryMultiValues)
            {
                await _registry.SetValueAsync(
                    multiValue.Key,
                    multiValue.ValueName,
                    ParseValueData(multiValue.ValueData, multiValue.ValueKind),
                    multiValue.ValueKind);
            }
        }
    }

    /// <summary>
    /// Parses the string value data from the definition into the appropriate type for the registry value kind.
    /// Uses uint for DWORD to handle values that exceed int.MaxValue (e.g. 0xFFFFFFFF = 4294967295).
    /// </summary>
    private static object ParseValueData(string? valueData, RegistryValueKind kind)
    {
        if (string.IsNullOrEmpty(valueData)) return 0;

        return kind switch
        {
            RegistryValueKind.DWord => unchecked((int)uint.Parse(valueData)),
            RegistryValueKind.QWord => unchecked((long)ulong.Parse(valueData)),
            RegistryValueKind.String => valueData,
            RegistryValueKind.MultiString => valueData.Split(';'),
            RegistryValueKind.Binary => Convert.FromBase64String(valueData),
            _ => unchecked((int)uint.Parse(valueData)),
        };
    }
}
