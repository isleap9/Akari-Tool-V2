// MemoryOperationTests — tests for MemoryOperationExecutor and memory tweak definitions.
//
// FakeMemoryManager tests are logic-only — PowerShell MMAgent operations require admin
// elevation and runtime verification (see PITFALLS.md Pitfall 2, D-04).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Memory;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Akari.Engine.Tweaks;
using Akari.Engine.Tweaks.Memory;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for MemoryOperationExecutor — tracer test for memory compression toggle (MEM-01).
/// Uses FakeMemoryManager (logic-only per D-04).
/// Runtime memory operations require admin elevation and log verification.
/// </summary>
public class MemoryOperationTests
{
    /// <summary>
    /// Tracer test: MemoryOperationExecutor applies MEM-01 (memory compression),
    /// calls DisableCompressionAsync, and updates state to Applied.
    /// </summary>
    [Fact]
    public async Task MemoryOperation_ApplyDisableCompression_DisablesViaPowerShell()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var memoryManager = new FakeMemoryManager(initialCompressionState: true);

        var executor = new MemoryOperationExecutor(memoryManager, stateService, logService);

        var definition = MemoryCompressionTweak.Definition;

        // Act
        var result = await executor.ApplyAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.Applied, result.Status);

        // Disable should have been called
        Assert.True(memoryManager.WasDisableCalled);
        Assert.False(memoryManager.CurrentCompressionEnabled);

        // State should be updated to Applied
        var status = await stateService.GetStatusAsync(definition.Id);
        Assert.Equal(TweakStatus.Applied, status);

        // Log should contain the operation
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Applying memory tweak: MEM-01", logContent);
        Assert.Contains("Disable-MMAgent", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: MemoryOperationExecutor.CanHandle returns true for Memory, false for others.
    /// </summary>
    [Fact]
    public void MemoryOperation_CanHandleMemoryTypeOnly()
    {
        var executor = new MemoryOperationExecutor(
            new FakeMemoryManager(),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "state.json")),
            new FileLogService(Path.GetTempPath()));

        Assert.True(executor.CanHandle(TweakType.Memory));
        Assert.False(executor.CanHandle(TweakType.Registry));
        Assert.False(executor.CanHandle(TweakType.Power));
        Assert.False(executor.CanHandle(TweakType.Service));
    }

    /// <summary>
    /// Test: Revert re-enables memory compression.
    /// </summary>
    [Fact]
    public async Task MemoryOperation_Revert_EnablesCompression()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var memoryManager = new FakeMemoryManager(initialCompressionState: false);

        var executor = new MemoryOperationExecutor(memoryManager, stateService, logService);

        var definition = MemoryCompressionTweak.Definition;

        // Act — Revert
        var result = await executor.RevertAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.NotApplied, result.Status);

        // Enable should have been called
        Assert.True(memoryManager.WasEnableCalled);
        Assert.True(memoryManager.CurrentCompressionEnabled);

        // State should be NotApplied
        var status = await stateService.GetStatusAsync(definition.Id);
        Assert.Equal(TweakStatus.NotApplied, status);

        // Log should contain the revert operation
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Reverting memory tweak: MEM-01", logContent);
        Assert.Contains("Enable-MMAgent", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: MemoryCompressionTweak definition has correct properties.
    /// </summary>
    [Fact]
    public void MemoryCompressionTweak_HasCorrectProperties()
    {
        var def = MemoryCompressionTweak.Definition;

        Assert.Equal("MEM-01", def.Id);
        Assert.Equal("Memory Compression", def.Name);
        Assert.Equal("Memory", def.Category);
        Assert.Equal(TweakType.Memory, def.Type);
        Assert.Equal("Disable-MMAgent -MemoryCompression", def.PowerShellCommand);
        Assert.Equal("Enable-MMAgent -MemoryCompression", def.PowerShellRevertCommand);
        Assert.True(def.RequiresAdmin);
        Assert.True(def.RequiresRestart);
        Assert.Equal(1, def.SortOrder);
    }

    /// <summary>
    /// Test: Apply returns failure when PowerShellCommand is missing.
    /// </summary>
    [Fact]
    public async Task MemoryOperation_ApplyWithMissingCommand_ReturnsFailure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var executor = new MemoryOperationExecutor(
            new FakeMemoryManager(),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(tempDir, "state.json")),
            new FileLogService(tempDir));

        var definition = new TweakDefinition
        {
            Id = "BAD-MEM",
            Name = "Bad Memory",
            Type = TweakType.Memory,
            // No PowerShellCommand
        };

        var result = await executor.ApplyAsync(definition);

        Assert.False(result.Success);
        Assert.Contains("missing", result.ErrorMessage!);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: Log file contains operation entry with target name and outcome.
    /// </summary>
    [Fact]
    public async Task MemoryOperation_LogContainsOperationEntry()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var memoryManager = new FakeMemoryManager(initialCompressionState: true);

        var executor = new MemoryOperationExecutor(memoryManager, stateService, logService);
        var definition = MemoryCompressionTweak.Definition;

        // Act
        await executor.ApplyAsync(definition);

        // Assert — log should contain operation name, target, and success
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("[Info]", logContent);
        Assert.Contains("MEM-01", logContent);
        Assert.Contains("disabled", logContent);

        // Also verify the file exists at the expected path
        Assert.True(File.Exists(logService.LogFilePath));

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