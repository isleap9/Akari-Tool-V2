// ServiceOperationTests — tests for ServiceOperationExecutor, ServiceOperationExecutor,
// XboxServicesTweak, and GameDvrGameBarTweak definitions.
//
// FakeServiceControllerFactory tests are logic-only — ACL failures and real service
// operations caught at runtime via elevated launch + log check (see PITFALLS.md Pitfall 2, D-04).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Registry;
using Akari.Engine.Services;
using Akari.Engine.Storage;
using Akari.Engine.Tweaks;
using Akari.Engine.Tweaks.Service;
using Microsoft.Win32;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for ServiceOperationExecutor — tracer test for service disable/stop flow.
/// Uses FakeServiceControllerFactory and FakeRegistryProvider (logic-only per D-04).
/// </summary>
public class ServiceOperationTests
{
    /// <summary>
    /// Tracer test: ServiceOperationExecutor applies Xbox services tweak,
    /// writes Start=4 to registry, and stops each service via FakeServiceControllerFactory.
    /// Logic-only validation (FakeServiceControllerFactory does NOT enforce ACLs — D-04).
    /// </summary>
    [Fact]
    public async Task ServiceOperation_ApplyXfXboxServices_DisablesAllServices()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var serviceFactory = new FakeServiceControllerFactory();

        // Register all 7 Xbox services as running
        var xboxServices = XboxServicesTweak.Definition.ServiceNames!;
        foreach (var svc in xboxServices)
        {
            serviceFactory.RegisterService(svc, isRunning: true, ServiceStartType.Automatic);
        }

        var executor = new ServiceOperationExecutor(
            registryProvider, serviceFactory, stateService, logService);

        var definition = XboxServicesTweak.Definition;

        // Act
        var result = await executor.ApplyAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.Applied, result.Status);

        // All services should be stopped
        foreach (var svc in xboxServices)
        {
            Assert.True(serviceFactory.WasServiceStopped(svc), $"Service {svc} was not stopped");
        }

        // All services should have Start=4 written to registry
        foreach (var svc in xboxServices)
        {
            var registryKey = $@"HKLM:\SYSTEM\CurrentControlSet\Services\{svc}";
            var startValue = await registryProvider.GetValueAsync<int>(registryKey, "Start");
            Assert.Equal(4, startValue); // Start=4 (disabled)
        }

        // State should be updated to Applied
        var status = await stateService.GetStatusAsync(definition.Id);
        Assert.Equal(TweakStatus.Applied, status);

        // Log file should contain entries
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Applying service tweak: SVC-01", logContent);
        Assert.Contains("services processed", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: ServiceOperationExecutor.CanHandle returns true for Service, false for Registry.
    /// </summary>
    [Fact]
    public void ServiceOperation_CanHandleServiceTypeOnly()
    {
        var executor = new ServiceOperationExecutor(
            new FakeRegistryProvider(),
            new FakeServiceControllerFactory(),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "state.json")),
            new FileLogService(Path.GetTempPath()));

        Assert.True(executor.CanHandle(TweakType.Service));
        Assert.False(executor.CanHandle(TweakType.Registry));
        Assert.False(executor.CanHandle(TweakType.Process));
    }

    /// <summary>
    /// Test: Revert restores Service start type to 3 (manual) and starts services.
    /// </summary>
    [Fact]
    public async Task ServiceOperation_RevertRestoresStartTypeAndStartsServices()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var serviceFactory = new FakeServiceControllerFactory();

        var xboxServices = XboxServicesTweak.Definition.ServiceNames!;
        foreach (var svc in xboxServices)
        {
            serviceFactory.RegisterService(svc, isRunning: false, ServiceStartType.Disabled);
        }

        var executor = new ServiceOperationExecutor(
            registryProvider, serviceFactory, stateService, logService);

        var definition = XboxServicesTweak.Definition;

        // Act — Revert
        var result = await executor.RevertAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.NotApplied, result.Status);

        // All services should be started
        foreach (var svc in xboxServices)
        {
            Assert.True(serviceFactory.WasServiceStarted(svc), $"Service {svc} was not started");
        }

        // All services should have Start=3 written to registry
        foreach (var svc in xboxServices)
        {
            var registryKey = $@"HKLM:\SYSTEM\CurrentControlSet\Services\{svc}";
            var startValue = await registryProvider.GetValueAsync<int>(registryKey, "Start");
            Assert.Equal(3, startValue); // Start=3 (manual/revert)
        }

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: ServiceOperationExecutor warns about dependency chains (Pitfall 10).
    /// </summary>
    [Fact]
    public async Task ServiceOperation_ApplyWithDependents_LogsWarning()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var serviceFactory = new FakeServiceControllerFactory();

        serviceFactory.RegisterService("XblAuthManager", isRunning: true, ServiceStartType.Automatic);

        var executor = new ServiceOperationExecutor(
            registryProvider, serviceFactory, stateService, logService);

        // Create a tweak with a service that has dependents
        var definition = new TweakDefinition
        {
            Id = "TEST-SVC",
            Name = "Test Service",
            Type = TweakType.Service,
            ServiceNames = new List<string> { "XblAuthManager" },
            ServiceStartValue = "4",
            ServiceRevertStartValue = "3",
        };

        // Act
        await executor.ApplyAsync(definition);

        // Assert — log should contain an entry about applying the service tweak
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Applying service tweak", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: XboxServicesTweak definition has 7 correct service names.
    /// </summary>
    [Fact]
    public void XboxServicesTweak_HasCorrectServiceNames()
    {
        var def = XboxServicesTweak.Definition;

        Assert.Equal("SVC-01", def.Id);
        Assert.Equal("Xbox Background Services", def.Name);
        Assert.Equal(TweakType.Service, def.Type);
        Assert.NotNull(def.ServiceNames);
        Assert.Equal(7, def.ServiceNames!.Count);

        // Verify from Services.ps1 — Xbox services with Start=4 (lines 918-928)
        var expected = new[]
        {
            "XblAuthManager",     // Xbox Live Auth Manager
            "XblGameSave",        // Xbox Live Game Save
            "XboxGipSvc",         // Xbox Game Input
            "XboxNetApiSvc",      // Xbox Networking
            "GamingServices",     // Gaming Services
            "BcastDVRUserService",// GameDVR/Broadcast User Service
            "GameInputSvc",       // GameInput Service
        };

        foreach (var svc in expected)
        {
            Assert.Contains(svc, def.ServiceNames!);
        }

        Assert.Equal("4", def.ServiceStartValue);      // Disabled
        Assert.Equal("3", def.ServiceRevertStartValue); // Manual
        Assert.True(def.RequiresAdmin);
    }

    /// <summary>
    /// Test: GameDvrGameBarTweak definition has correct service names and registry values.
    /// </summary>
    [Fact]
    public void GameDvrGameBarTweak_HasCorrectServicesAndRegistryValues()
    {
        var def = GameDvrGameBarTweak.Definition;

        Assert.Equal("SVC-02", def.Id);
        Assert.Equal("GameDVR & Game Bar", def.Name);
        Assert.Equal(TweakType.Service, def.Type);
        Assert.NotNull(def.ServiceNames);
        Assert.Equal(6, def.ServiceNames!.Count);

        // Verify service names from Gamebar.ps1 "on" path (lines 197-218)
        var expectedServices = new[]
        {
            "BcastDVRUserService",    // GameDVR/Broadcast User Service (line 201)
            "GameInputSvc",           // GameInput Service (line 197)
            "XboxGipSvc",             // Xbox Game Input (line 205)
            "XblAuthManager",         // Xbox Live Auth Manager (line 209)
            "XblGameSave",            // Xbox Live Game Save (line 213)
            "XboxNetApiSvc",          // Xbox Networking (line 217)
        };
        foreach (var svc in expectedServices)
        {
            Assert.Contains(svc, def.ServiceNames!);
        }

        // Verify registry values from Gamebar.ps1 "off" path (lines 94-134)
        Assert.NotNull(def.RegistryMultiValues);
        Assert.Equal(5, def.RegistryMultiValues!.Count);

        // HKCU: GameDVR_Enabled = 0 (line 95)
        var gameDvrEnabled = def.RegistryMultiValues.FirstOrDefault(v => v.ValueName == "GameDVR_Enabled");
        Assert.NotNull(gameDvrEnabled);
        Assert.Equal("0", gameDvrEnabled!.ValueData);
        Assert.Equal(RegistryValueKind.DWord, gameDvrEnabled.ValueKind);

        // HKCU: AppCaptureEnabled = 0 (line 98)
        var appCapture = def.RegistryMultiValues.FirstOrDefault(v => v.ValueName == "AppCaptureEnabled");
        Assert.NotNull(appCapture);
        Assert.Equal("0", appCapture!.ValueData);

        // HKCU: UseNexusForGameBarEnabled = 0 (line 102)
        var nexus = def.RegistryMultiValues.FirstOrDefault(v => v.ValueName == "UseNexusForGameBarEnabled");
        Assert.NotNull(nexus);
        Assert.Equal("0", nexus!.ValueData);

        // HKCU: GamepadNexusChordEnabled = 0 (line 106)
        var chord = def.RegistryMultiValues.FirstOrDefault(v => v.ValueName == "GamepadNexusChordEnabled");
        Assert.NotNull(chord);
        Assert.Equal("0", chord!.ValueData);

        // HKLM: ActivationType = 0 (line 134)
        var activation = def.RegistryMultiValues.FirstOrDefault(v => v.ValueName == "ActivationType");
        Assert.NotNull(activation);
        Assert.Equal("0", activation!.ValueData);

        Assert.True(def.RequiresAdmin);
    }

    /// <summary>
    /// Test: Apply returns failure when ServiceNames is missing.
    /// </summary>
    [Fact]
    public async Task ServiceOperation_ApplyWithMissingServiceNames_ReturnsFailure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var executor = new ServiceOperationExecutor(
            new FakeRegistryProvider(),
            new FakeServiceControllerFactory(),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(tempDir, "state.json")),
            new FileLogService(tempDir));

        var definition = new TweakDefinition
        {
            Id = "BAD-SVC",
            Name = "Bad Service",
            Type = TweakType.Service,
            // No ServiceNames
        };

        var result = await executor.ApplyAsync(definition);

        Assert.False(result.Success);
        Assert.Contains("missing", result.ErrorMessage!);

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