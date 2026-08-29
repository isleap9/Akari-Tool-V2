// ServiceOperationExecutor — ITweakExecutor implementation for service-based tweaks.
//
// Dispatches apply/revert for TweakType.Service. For each service in the tweak
// definition's ServiceNames list, it:
//   1. Writes Start=4 (disabled) to the service's registry key via IRegistryProvider
//      (2-arg OpenSubKey pattern from Phase 1 — D-01, Pitfall 1)
//   2. Checks dependency chains via IServiceControllerFactory (Pitfall 10)
//   3. Calls StopAsync via the service controller
//
// Revert reverses both operations: Start=3 (manual) + StartAsync.
// All operations are async Task with Task.Run offloading (D-11, Pitfall 5).
// All operations logged via ILogService (T-04-02 mitigation).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Registry;
using Akari.Engine.Services;
using Akari.Engine.Storage;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks;

/// <summary>
/// Executor for <see cref="TweakType.Service"/> tweaks. Manages Windows service
/// start/stop and disable/re-enable via the registry Start value + ServiceController.
/// Uses <see cref="IRegistryProvider"/> for registry writes (2-arg OpenSubKey, D-01)
/// and <see cref="IServiceControllerFactory"/> for runtime service control.
/// </summary>
public class ServiceOperationExecutor : ITweakExecutor
{
    private readonly IRegistryProvider _registry;
    private readonly IServiceControllerFactory _serviceControllerFactory;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public ServiceOperationExecutor(
        IRegistryProvider registry,
        IServiceControllerFactory serviceControllerFactory,
        ITweakStateService stateService,
        ILogService logService)
    {
        _registry = registry;
        _serviceControllerFactory = serviceControllerFactory;
        _stateService = stateService;
        _logService = logService;
    }

    /// <inheritdoc/>
    public bool CanHandle(TweakType type) => type == TweakType.Service;

    /// <inheritdoc/>
    public async Task<TweakResult> ApplyAsync(TweakDefinition definition)
    {
        return await Task.Run(async () =>
        {
            try
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Applying service tweak: {definition.Id} ({definition.Name})");

                if (definition.ServiceNames == null || definition.ServiceNames.Count == 0)
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing ServiceNames"
                    };
                }

                var startValue = definition.ServiceStartValue ?? "4";
                var startType = ParseStartType(startValue);
                var revertStartValue = definition.ServiceRevertStartValue ?? "3";
                var revertStartType = ParseStartType(revertStartValue);

                var results = new List<bool>();
                foreach (var serviceName in definition.ServiceNames)
                {
                    var success = await ApplyServiceAsync(serviceName, startType, revertStartType,
                        startValue, revertStartValue);
                    results.Add(success);
                }

                var allSuccess = results.All(r => r);
                await _stateService.UpdateAsync(definition.Id,
                    allSuccess ? TweakStatus.Applied : TweakStatus.NotApplied);

                await _logService.LogAsync(LogLevel.Info,
                    $"Service tweak {definition.Id}: {results.Count(r => r)}/{results.Count} services processed");

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = allSuccess,
                    Status = allSuccess ? TweakStatus.Applied : TweakStatus.NotApplied,
                    ErrorMessage = allSuccess ? null : $"Partial failure: {results.Count(r => !r)} service(s) failed"
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to apply service tweak {definition.Id}: {ex.Message}", ex);
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
                    $"Reverting service tweak: {definition.Id} ({definition.Name})");

                if (definition.ServiceNames == null || definition.ServiceNames.Count == 0)
                {
                    return new TweakResult
                    {
                        TweakId = definition.Id,
                        Success = false,
                        Status = TweakStatus.NotApplied,
                        ErrorMessage = "Tweak definition missing ServiceNames"
                    };
                }

                var revertStartValue = definition.ServiceRevertStartValue ?? "3";
                var revertStartType = ParseStartType(revertStartValue);

                var results = new List<bool>();
                foreach (var serviceName in definition.ServiceNames)
                {
                    var success = await RevertServiceAsync(serviceName, revertStartType, revertStartValue);
                    results.Add(success);
                }

                var allSuccess = results.All(r => r);
                await _stateService.UpdateAsync(definition.Id,
                    allSuccess ? TweakStatus.NotApplied : TweakStatus.Applied);

                await _logService.LogAsync(LogLevel.Info,
                    $"Service tweak {definition.Id} reverted: {results.Count(r => r)}/{results.Count} services processed");

                return new TweakResult
                {
                    TweakId = definition.Id,
                    Success = allSuccess,
                    Status = allSuccess ? TweakStatus.NotApplied : TweakStatus.Applied,
                    ErrorMessage = allSuccess ? null : $"Partial failure: {results.Count(r => !r)} service(s) failed"
                };
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync(
                    $"Failed to revert service tweak {definition.Id}: {ex.Message}", ex);
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
    /// Applies the service tweak to a single service: checks dependencies (Pitfall 10),
    /// writes Start=disabled to registry via IRegistryProvider, then stops the service.
    /// </summary>
    private async Task<bool> ApplyServiceAsync(
        string serviceName,
        ServiceStartType disabledStartType,
        ServiceStartType revertStartType,
        string startValue,
        string revertStartValue)
    {
        try
        {
            // Check dependency chains (Pitfall 10) — warn before stopping a service with dependents
            var dependents = await _serviceControllerFactory.GetDependentServicesAsync(serviceName);
            var dependentList = dependents.ToList();
            if (dependentList.Any())
            {
                await _logService.LogAsync(LogLevel.Warning,
                    $"Service {serviceName} has dependents: {string.Join(", ", dependentList)}. Stopping may affect them.");
            }

            // Write Start=4 (disabled) to registry via IRegistryProvider
            // Uses 2-arg OpenSubKey(path, true) pattern — D-01, Pitfall 1
            var registryKey = $@"HKLM:\SYSTEM\CurrentControlSet\Services\{serviceName}";
            await _registry.SetValueAsync(
                registryKey,
                "Start",
                (int)disabledStartType,
                RegistryValueKind.DWord);

            await _logService.LogAsync(LogLevel.Info,
                $"Service {serviceName}: written Start={startValue} to registry");

            // Stop the service via IServiceControllerFactory (runtime control)
            var controller = await _serviceControllerFactory.GetServiceControllerAsync(serviceName);
            if (controller != null)
            {
                if (controller.IsRunning)
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Service {serviceName}: stopping (runtime)");

                    var stopped = await controller.StopAsync();
                    if (!stopped)
                    {
                        await _logService.LogAsync(LogLevel.Warning,
                            $"Service {serviceName}: failed to stop at runtime (will take effect on next reboot)");
                    }
                    else
                    {
                        await _logService.LogAsync(LogLevel.Info,
                            $"Service {serviceName}: stopped successfully");
                    }
                }
                else
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Service {serviceName}: already stopped");
                }
            }
            else
            {
                await _logService.LogAsync(LogLevel.Warning,
                    $"Service {serviceName}: not found on this system (registry Start value still written for persistence)");
            }

            return true;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Failed to apply service {serviceName}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Reverts the service tweak: writes Start=manual to registry, then starts the service.
    /// </summary>
    private async Task<bool> RevertServiceAsync(
        string serviceName,
        ServiceStartType revertStartType,
        string revertStartValue)
    {
        try
        {
            // Write Start=3 (manual) to registry via IRegistryProvider
            var registryKey = $@"HKLM:\SYSTEM\CurrentControlSet\Services\{serviceName}";
            await _registry.SetValueAsync(
                registryKey,
                "Start",
                (int)revertStartType,
                RegistryValueKind.DWord);

            await _logService.LogAsync(LogLevel.Info,
                $"Service {serviceName}: written Start={revertStartValue} to registry (revert)");

            // Start the service if it's not running
            var controller = await _serviceControllerFactory.GetServiceControllerAsync(serviceName);
            if (controller != null && !controller.IsRunning)
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Service {serviceName}: starting (runtime revert)");

                var started = await controller.StartAsync();
                if (!started)
                {
                    await _logService.LogAsync(LogLevel.Warning,
                        $"Service {serviceName}: failed to start (registry revert applied — will start on next boot)");
                }
                else
                {
                    await _logService.LogAsync(LogLevel.Info,
                        $"Service {serviceName}: started successfully");
                }
            }
            else if (controller != null)
            {
                await _logService.LogAsync(LogLevel.Info,
                    $"Service {serviceName}: already running");
            }
            else
            {
                await _logService.LogAsync(LogLevel.Warning,
                    $"Service {serviceName}: not found on this system (registry revert applied)");
            }

            return true;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Failed to revert service {serviceName}: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Parses a string start type value to the ServiceStartType enum.
    /// </summary>
    private static ServiceStartType ParseStartType(string value)
    {
        if (int.TryParse(value, out var intVal))
        {
            return (ServiceStartType)intVal;
        }
        return ServiceStartType.Disabled;
    }
}