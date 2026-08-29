// PowerOperationTests — tests for PowerOperationExecutor and power tweak definitions.
//
// FakePowerManager tests are logic-only — powercfg operations require admin
// elevation and runtime verification (see PITFALLS.md Pitfall 2, D-04).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Power;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Akari.Engine.Tweaks;
using Akari.Engine.Tweaks.Power;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for PowerOperationExecutor — tracer test for power plan activation (PWR-01, PWR-02).
/// Uses FakePowerManager (logic-only per D-04).
/// Runtime power operations require admin elevation and log verification.
/// </summary>
public class PowerOperationTests
{
    /// <summary>
    /// Tracer test: PowerOperationExecutor applies PWR-01 (Ultimate Performance),
    /// checks if base scheme exists, duplicates it, and activates via FakePowerManager.
    /// </summary>
    [Fact]
    public async Task PowerOperation_ApplyUltimatePerformance_ActivatesScheme()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var powerManager = new FakePowerManager(registryProvider);

        // Register the Ultimate Performance base scheme so duplication succeeds
        powerManager.RegisterScheme(
            UltimatePerformanceTweak.BaseSchemeGuid,
            "Ultimate Performance",
            isActive: false);

        var executor = new PowerOperationExecutor(powerManager, stateService, logService);

        var definition = UltimatePerformanceTweak.Definition;

        // Act
        var result = await executor.ApplyAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.Applied, result.Status);

        // The target scheme should have been activated
        Assert.True(powerManager.WasSchemeActivated(definition.PowerSchemeGuid));

        // State should be updated to Applied
        var status = await stateService.GetStatusAsync(definition.Id);
        Assert.Equal(TweakStatus.Applied, status);

        // Log should contain the operation
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Applying power tweak: PWR-01", logContent);
        Assert.Contains("duplicate", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: PowerOperationExecutor.CanHandle returns true for Power, false for others.
    /// </summary>
    [Fact]
    public void PowerOperation_CanHandlePowerTypeOnly()
    {
        var executor = new PowerOperationExecutor(
            new FakePowerManager(new FakeRegistryProvider()),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "state.json")),
            new FileLogService(Path.GetTempPath()));

        Assert.True(executor.CanHandle(TweakType.Power));
        Assert.False(executor.CanHandle(TweakType.Registry));
        Assert.False(executor.CanHandle(TweakType.Service));
        Assert.False(executor.CanHandle(TweakType.Process));
    }

    /// <summary>
    /// Test: Fallback to High Performance when Ultimate Performance not available (Pitfall 9).
    /// </summary>
    [Fact]
    public async Task PowerOperation_ApplyWithFallback_SwitchesToHighPerformance()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var powerManager = new FakePowerManager(registryProvider);

        // Do NOT register the Ultimate Performance base scheme — it won't be found.
        // The executor should fall back to High Performance.
        // Register the High Performance fallback scheme so activation succeeds.
        powerManager.RegisterScheme(
            "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
            "High Performance",
            isActive: false);

        var executor = new PowerOperationExecutor(powerManager, stateService, logService);

        var definition = UltimatePerformanceTweak.Definition;

        // Act
        var result = await executor.ApplyAsync(definition);

        // Assert
        // Even though Ultimate Performance base isn't found, the fallback should succeed
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.Applied, result.Status);

        // Should have been called with the fallback High Performance GUID
        Assert.Equal("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", powerManager.ActiveSchemeGuid);

        // Log should contain the fallback warning
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Falling back to High Performance", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: Revert restores default power schemes.
    /// </summary>
    [Fact]
    public async Task PowerOperation_Revert_RestoresDefaultSchemes()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var powerManager = new FakePowerManager(registryProvider);

        powerManager.RegisterScheme(
            UltimatePerformanceTweak.TargetSchemeGuid,
            "Copy of Ultimate Performance",
            isActive: true);

        var executor = new PowerOperationExecutor(powerManager, stateService, logService);

        var definition = UltimatePerformanceTweak.Definition;

        // Act — Revert
        var result = await executor.RevertAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.NotApplied, result.Status);

        // RestoreDefaultSchemesAsync should have been called
        Assert.True(powerManager.WasRestoreDefaultsCalled);

        // State should be NotApplied
        var status = await stateService.GetStatusAsync(definition.Id);
        Assert.Equal(TweakStatus.NotApplied, status);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: UltimatePerformanceTweak definition has correct GUIDs.
    /// </summary>
    [Fact]
    public void UltimatePerformanceTweak_HasCorrectGuids()
    {
        var def = UltimatePerformanceTweak.Definition;

        Assert.Equal("PWR-01", def.Id);
        Assert.Equal("Ultimate Performance", def.Name);
        Assert.Equal("Power", def.Category);
        Assert.Equal(TweakType.Power, def.Type);
        Assert.Equal(UltimatePerformanceTweak.TargetSchemeGuid, def.PowerSchemeGuid);
        Assert.Equal(UltimatePerformanceTweak.BaseSchemeGuid, def.PowerBaseSchemeGuid);
        Assert.True(def.RequiresAdmin);
        Assert.False(def.RequiresRestart);
        Assert.Equal(1, def.SortOrder);
    }

    /// <summary>
    /// Test: HighPerformanceTweak definition has correct GUID.
    /// </summary>
    [Fact]
    public void HighPerformanceTweak_HasCorrectGuid()
    {
        var def = HighPerformanceTweak.Definition;

        Assert.Equal("PWR-02", def.Id);
        Assert.Equal("High Performance (Fallback)", def.Name);
        Assert.Equal("Power", def.Category);
        Assert.Equal(TweakType.Power, def.Type);
        Assert.Equal("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", def.PowerSchemeGuid);
        Assert.True(def.RequiresAdmin);
        Assert.False(def.RequiresRestart);
        Assert.Equal(2, def.SortOrder);
    }

    /// <summary>
    /// Test: Apply returns failure when PowerSchemeGuid is missing.
    /// </summary>
    [Fact]
    public async Task PowerOperation_ApplyWithMissingGuid_ReturnsFailure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var executor = new PowerOperationExecutor(
            new FakePowerManager(new FakeRegistryProvider()),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(tempDir, "state.json")),
            new FileLogService(tempDir));

        var definition = new TweakDefinition
        {
            Id = "BAD-PWR",
            Name = "Bad Power",
            Type = TweakType.Power,
            // No PowerSchemeGuid
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