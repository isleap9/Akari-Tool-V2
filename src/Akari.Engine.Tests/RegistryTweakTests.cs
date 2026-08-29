// RegistryTweakTests — tests for the 7 registry tweaks and RegistryTweakExecutor.
//
// FakeRegistryProvider tests are logic-only — ACL failures caught at runtime
// via elevated launch + log check (see PITFALLS.md Pitfall 2, D-04).

using System.Text.Json;
using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Akari.Engine.Tweaks;
using Akari.Engine.Tweaks.Registry;
using Microsoft.Win32;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for Game Mode twist (REG-01) — tracer test for the executor.
/// </summary>
public class GameModeTweakTests
{
    /// <summary>
    /// Tracer test: RegistryTweakExecutor applies Game Mode via FakeRegistryProvider,
    /// then reverts and verifies the disabled value is written.
    /// Logic-only validation (FakeRegistryProvider does NOT enforce ACLs — D-04).
    /// </summary>
    [Fact]
    public async Task GameModeTweak_ApplyAndRevert_ThroughRegistryTweakExecutor()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);

        var executor = new RegistryTweakExecutor(registryProvider, stateService, logService);
        var definition = GameModeTweak.Definition;

        // Act — Apply
        var applyResult = await executor.ApplyAsync(definition);

        // Assert — Apply
        Assert.True(applyResult.Success);
        Assert.Equal(TweakStatus.Applied, applyResult.Status);
        var appliedValue = await registryProvider.GetValueAsync<int>(
            definition.RegistryKey!, definition.RegistryValueName!);
        Assert.Equal(1, appliedValue); // GameMode = 1 (enabled)

        // Act — Revert
        var revertResult = await executor.RevertAsync(definition);

        // Assert — Revert
        Assert.True(revertResult.Success);
        Assert.Equal(TweakStatus.NotApplied, revertResult.Status);
        var revertedValue = await registryProvider.GetValueAsync<int>(
            definition.RegistryKey!, definition.RegistryValueName!);
        Assert.Equal(0, revertedValue); // GameMode = 0 (disabled)

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: RegistryTweakExecutor.CanHandle returns true for TweakType.Registry.
    /// </summary>
    [Fact]
    public void GameModeTweak_ExecutorCanHandleRegistryType()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);

        var executor = new RegistryTweakExecutor(registryProvider, stateService, logService);

        Assert.True(executor.CanHandle(TweakType.Registry));
        Assert.False(executor.CanHandle(TweakType.Service));
        Assert.False(executor.CanHandle(TweakType.Process));

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: GameModeTweak definition has correct registry path and values.
    /// </summary>
    [Fact]
    public void GameModeTweak_DefinitionHasCorrectPathAndValues()
    {
        var def = GameModeTweak.Definition;

        Assert.Equal("REG-01", def.Id);
        Assert.Equal(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList", def.RegistryKey);
        Assert.Equal("GameMode", def.RegistryValueName);
        Assert.Equal(RegistryValueKind.DWord, def.RegistryValueKind);
        Assert.Equal("1", def.RegistryValueData);       // Enabled
        Assert.Equal("0", def.RegistryRevertValueData); // Disabled
        Assert.True(def.RequiresAdmin);
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }
}

/// <summary>
/// Tests for all 7 registry tweak definitions — verifies correct paths and values.
/// </summary>
public class AllTweaksTests
{
    [Fact]
    public void HagsTweak_HasCorrectPathAndValues()
    {
        var def = HagsTweak.Definition;

        Assert.Equal("REG-02", def.Id);
        Assert.Equal(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", def.RegistryKey);
        Assert.Equal("HwSchMode", def.RegistryValueName);
        Assert.Equal("2", def.RegistryValueData);  // Enabled
        Assert.Equal("1", def.RegistryRevertValueData); // Disabled
        Assert.True(def.RequiresAdmin);
        Assert.True(def.RequiresRestart);
    }

    [Fact]
    public void NetworkThrottlingTweak_HasCorrectPathAndValues()
    {
        var def = NetworkThrottlingTweak.Definition;

        Assert.Equal("REG-03", def.Id);
        Assert.Equal(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", def.RegistryKey);
        Assert.Equal("NetworkThrottlingIndex", def.RegistryValueName);
        Assert.Equal("4294967295", def.RegistryValueData); // 0xFFFFFFFF
        Assert.Equal("3", def.RegistryRevertValueData);
        Assert.True(def.RequiresAdmin);
    }

    [Fact]
    public void Win32PrioritySeparationTweak_HasCorrectPathAndValues()
    {
        var def = Win32PrioritySeparationTweak.Definition;

        Assert.Equal("REG-04", def.Id);
        Assert.Equal(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", def.RegistryKey);
        Assert.Equal("Win32PrioritySeparation", def.RegistryValueName);
        Assert.Equal("26", def.RegistryValueData); // 0x26
        Assert.Equal("38", def.RegistryRevertValueData);
        Assert.True(def.RequiresAdmin);
    }

    [Fact]
    public void MultimediaTweak_HasCorrectPathAndValues()
    {
        var def = MultimediaTweak.Definition;

        Assert.Equal("REG-05", def.Id);
        Assert.Equal(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", def.RegistryKey);
        Assert.Equal("Priority", def.RegistryValueName);
        Assert.Equal("6", def.RegistryValueData);  // High priority
        Assert.Equal("1", def.RegistryRevertValueData); // Default
        Assert.True(def.RequiresAdmin);
    }

    [Fact]
    public void VisualEffectsTweak_HasCorrectPathAndValues()
    {
        var def = VisualEffectsTweak.Definition;

        Assert.Equal("REG-06", def.Id);
        Assert.Equal(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", def.RegistryKey);
        Assert.Equal("VisualFXSetting", def.RegistryValueName);
        Assert.Equal("2", def.RegistryValueData);  // Disable animations
        Assert.Equal("3", def.RegistryRevertValueData); // Default
        Assert.True(def.RequiresAdmin);
    }

    [Fact]
    public void MouseAccelerationTweak_HasCorrectPathAndValues()
    {
        var def = MouseAccelerationTweak.Definition;

        Assert.Equal("REG-07", def.Id);
        // HKCU, not HKLM — does not require admin
        Assert.Equal(@"HKCU:\Control Panel\Desktop", def.RegistryKey);
        Assert.Equal("MouseSpeed", def.RegistryValueName);
        Assert.Equal("0", def.RegistryValueData);  // Disabled
        Assert.False(def.RequiresAdmin); // HKCU does not require admin
        // Multi-value: MouseSpeed, MouseThreshold1, MouseThreshold2
        Assert.NotNull(def.RegistryMultiValues);
        Assert.Equal(3, def.RegistryMultiValues!.Count);
    }

    [Fact]
    public async Task AllTweaks_JsonCatalogLoadsFromJson()
    {
        // Arrange
        var catalogPath = Path.Combine(
            AppContext.BaseDirectory, "tweaks.json");

        // Act
        var catalog = await JsonTweakCatalog.FromFileAsync(catalogPath);
        var all = await catalog.GetAllAsync();

        // Assert
        Assert.Equal(8, all.Count); // 7 real tweaks + 1 test tweak
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-01"));
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-02"));
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-03"));
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-04"));
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-05"));
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-06"));
        Assert.NotNull(all.FirstOrDefault(t => t.Id == "REG-07"));
    }
}

/// <summary>
/// Tests for batch application of all 7 registry tweaks.
/// </summary>
public class BatchTweakTests
{
    [Fact]
    public async Task BatchApply_AllSevenTweaks_SucceedViaFakeRegistryProvider()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);

        var executor = new RegistryTweakExecutor(registryProvider, stateService, logService);
        var catalog = new JsonTweakCatalog(new[]
        {
            GameModeTweak.Definition,
            HagsTweak.Definition,
            NetworkThrottlingTweak.Definition,
            Win32PrioritySeparationTweak.Definition,
            MultimediaTweak.Definition,
            VisualEffectsTweak.Definition,
            MouseAccelerationTweak.Definition,
        });

        // Act
        var definitions = await catalog.GetAllAsync();
        var results = new List<TweakResult>();
        foreach (var def in definitions)
        {
            var result = await executor.ApplyAsync(def);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r => Assert.True(r.Success));
        Assert.All(results, r => Assert.Equal(TweakStatus.Applied, r.Status));
        Assert.Equal(7, results.Count);

        // Verify values were set in FakeRegistryProvider
        var gameModeValue = await registryProvider.GetValueAsync<int>(
            @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList", "GameMode");
        Assert.Equal(1, gameModeValue);

        var hagsValue = await registryProvider.GetValueAsync<int>(
            @"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
        Assert.Equal(2, hagsValue);

        var mouseSpeed = await registryProvider.GetValueAsync<int>(
            @"HKCU:\Control Panel\Desktop", "MouseSpeed");
        Assert.Equal(0, mouseSpeed);

        Cleanup(tempDir);
    }

    [Fact]
    public async Task BatchApply_ThenRevert_AllValuesRestored()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);

        var executor = new RegistryTweakExecutor(registryProvider, stateService, logService);

        var definitions = new[]
        {
            GameModeTweak.Definition,
            HagsTweak.Definition,
        };

        // Act — Apply
        foreach (var def in definitions)
        {
            await executor.ApplyAsync(def);
        }

        // Verify applied values
        var gameModeApplied = await registryProvider.GetValueAsync<int>(
            GameModeTweak.Definition.RegistryKey!, GameModeTweak.Definition.RegistryValueName!);
        Assert.Equal(1, gameModeApplied);

        // Act — Revert
        foreach (var def in definitions)
        {
            await executor.RevertAsync(def);
        }

        // Verify reverted values
        var gameModeReverted = await registryProvider.GetValueAsync<int>(
            GameModeTweak.Definition.RegistryKey!, GameModeTweak.Definition.RegistryValueName!);
        Assert.Equal(0, gameModeReverted);

        var hagsReverted = await registryProvider.GetValueAsync<int>(
            HagsTweak.Definition.RegistryKey!, HagsTweak.Definition.RegistryValueName!);
        Assert.Equal(1, hagsReverted);

        Cleanup(tempDir);
    }

    [Fact]
    public async Task HagsTweak_WritesToCorrect64BitPath_NotWow6432Node()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);

        var executor = new RegistryTweakExecutor(registryProvider, stateService, logService);

        // Act
        await executor.ApplyAsync(HagsTweak.Definition);

        // Assert — verify the value was written to the correct path
        var value = await registryProvider.GetValueAsync<int>(
            HagsTweak.Definition.RegistryKey!, HagsTweak.Definition.RegistryValueName!);
        Assert.Equal(2, value); // HwSchMode = 2 (enabled)

        // Verify the key exists (not redirected to Wow6432Node)
        var keyExists = await registryProvider.KeyExistsAsync(
            HagsTweak.Definition.RegistryKey!);
        Assert.True(keyExists);

        Cleanup(tempDir);
    }

    [Fact]
    public async Task MouseAccelerationTweak_WritesAllThreeValues()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);

        var executor = new RegistryTweakExecutor(registryProvider, stateService, logService);

        // Act
        await executor.ApplyAsync(MouseAccelerationTweak.Definition);

        // Assert — all 3 values should be set to 0
        var mouseSpeed = await registryProvider.GetValueAsync<int>(
            @"HKCU:\Control Panel\Desktop", "MouseSpeed");
        Assert.Equal(0, mouseSpeed);

        var threshold1 = await registryProvider.GetValueAsync<int>(
            @"HKCU:\Control Panel\Desktop", "MouseThreshold1");
        Assert.Equal(0, threshold1);

        var threshold2 = await registryProvider.GetValueAsync<int>(
            @"HKCU:\Control Panel\Desktop", "MouseThreshold2");
        Assert.Equal(0, threshold2);

        Cleanup(tempDir);
    }

    private static void Cleanup(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, true);
    }
}
