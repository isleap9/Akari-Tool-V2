// StateServiceTests — tests JSON persistence and startup re-validation.
//
// FakeRegistryProvider tests are logic-only — ACL failures caught at runtime
// via elevated launch + log check (see PITFALLS.md Pitfall 2, D-04).

using Akari.Engine.Core.Models;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Microsoft.Win32;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for JsonFileStateService — JSON persistence, status retrieval, and
/// the RevalidateAsync startup re-validation logic (D-05, ENG-06).
/// </summary>
public class StateServiceTests
{
    /// <summary>
    /// Test: GetStatusAsync returns NotApplied for a tweak that was never set.
    /// </summary>
    [Fact]
    public async Task StateService_GetStatusAsync_ReturnsNotAppliedForUnknownTweak()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));

        // Act
        var status = await stateService.GetStatusAsync("UNKNOWN");

        // Assert
        Assert.Equal(TweakStatus.NotApplied, status);
        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: UpdateAsync persists status, and GetAllStatusAsync returns it.
    /// </summary>
    [Fact]
    public async Task StateService_UpdateAndRetrieve_PersistsToJson()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateFile = Path.Combine(tempDir, "state.json");
        var stateService = new JsonFileStateService(registryProvider, stateFile);

        // Act
        await stateService.UpdateAsync("TEST-01", TweakStatus.Applied);
        await stateService.UpdateAsync("TEST-02", TweakStatus.NotApplied);

        var all = await stateService.GetAllStatusAsync();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Equal(TweakStatus.Applied, all["TEST-01"]);
        Assert.Equal(TweakStatus.NotApplied, all["TEST-02"]);

        // Verify the JSON file was actually written
        Assert.True(File.Exists(stateFile));
        var json = await File.ReadAllTextAsync(stateFile);
        Assert.Contains("TEST-01", json);
        Assert.Contains("Applied", json);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: RevalidateAsync detects Windows Update reverts — when the actual
    /// registry value doesn't match the expected value, status is reset to NotApplied.
    /// </summary>
    [Fact]
    public async Task StateService_RevalidateAsync_DetectsRevertedTweak()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();

        // Set the tweak as Applied in state
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        await stateService.UpdateAsync("REG-01", TweakStatus.Applied);

        // Set the registry value to a DIFFERENT value (simulating Windows Update revert)
        // The tweak definition expects value "1" but the registry now has "0"
        var registryProvider2 = new FakeRegistryProvider();
        await registryProvider2.SetValueAsync(
            "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameList",
            "GameMode", 0, RegistryValueKind.DWord);
        var stateService2 = new JsonFileStateService(registryProvider2,
            Path.Combine(tempDir, "state.json"));

        var tweaks = new[]
        {
            new TweakDefinition
            {
                Id = "REG-01",
                Name = "Game Mode",
                Category = "Registry",
                Type = TweakType.Registry,
                RegistryKey = "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameList",
                RegistryValueName = "GameMode",
                RegistryValueKind = RegistryValueKind.DWord,
                RegistryValueData = "1", // Expected value
            },
        };

        // Act
        var reverted = await stateService2.RevalidateAsync(tweaks);

        // Assert
        Assert.Contains("REG-01", reverted);
        var statusAfter = await stateService2.GetStatusAsync("REG-01");
        Assert.Equal(TweakStatus.NotApplied, statusAfter);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: RevalidateAsync returns empty list when registry value matches expected.
    /// </summary>
    [Fact]
    public async Task StateService_RevalidateAsync_NoRevertWhenValuesMatch()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();

        // Set the expected registry value
        await registryProvider.SetValueAsync(
            "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameList",
            "GameMode", 1, RegistryValueKind.DWord);

        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        await stateService.UpdateAsync("REG-01", TweakStatus.Applied);

        var tweaks = new[]
        {
            new TweakDefinition
            {
                Id = "REG-01",
                RegistryKey = "HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\GameList",
                RegistryValueName = "GameMode",
                RegistryValueKind = RegistryValueKind.DWord,
                RegistryValueData = "1", // Expected value matches
            },
        };

        // Act
        var reverted = await stateService.RevalidateAsync(tweaks);

        // Assert
        Assert.Empty(reverted);
        var statusAfter = await stateService.GetStatusAsync("REG-01");
        Assert.Equal(TweakStatus.Applied, statusAfter);

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
