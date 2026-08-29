// ProcessOperationTests — tests for ProcessOperationExecutor and process tweak definitions.
//
// FakeProcessManager tests are logic-only — real process operations require admin
// elevation and runtime verification (see PITFALLS.md Pitfall 2, D-04).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Akari.Engine.Tweaks;
using Akari.Engine.Tweaks.Process;
using Akari.Engine.Processes;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for ProcessOperationExecutor — tracer test for process priority management (PROC-01)
/// and background process killing (PROC-02).
/// Uses FakeProcessManager (logic-only per D-04). Runtime process operations require
/// admin elevation and log verification.
/// </summary>
public class ProcessOperationTests
{
    /// <summary>
    /// Tracer test: ProcessOperationExecutor applies PROC-01 (process priority tweak),
    /// sets process priority to High via FakeProcessManager.
    /// </summary>
    [Fact]
    public async Task ProcessOperation_ApplyProcessPriority_SetsHighPriority()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var processManager = new FakeProcessManager();

        // Register a process to be managed
        processManager.RegisterProcess("gam.exe");

        var executor = new ProcessOperationExecutor(processManager, stateService, logService);

        var definition = new TweakDefinition
        {
            Id = "PROC-01",
            Name = "Game Process Priority",
            Type = TweakType.Process,
            ProcessNames = new List<string> { "gam.exe" },
            ProcessPriority = "High",
            RequiresRestart = false,
            RequiresAdmin = true,
            SortOrder = 1,
        };

        // Act
        var result = await executor.ApplyAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.Applied, result.Status);

        // Process priority should have been set to High
        var priority = processManager.GetPriority("gam.exe");
        Assert.NotNull(priority);
        Assert.Equal("High", priority);

        // State should be updated
        var status = await stateService.GetStatusAsync(definition.Id);
        Assert.Equal(TweakStatus.Applied, status);

        // Log should contain the operation
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("Applying process tweak: PROC-01", logContent);
        Assert.Contains("High", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: ProcessOperationExecutor.CanHandle returns true for Process, false for Registry.
    /// </summary>
    [Fact]
    public void ProcessOperation_CanHandleProcessTypeOnly()
    {
        var executor = new ProcessOperationExecutor(
            new FakeProcessManager(),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "state.json")),
            new FileLogService(Path.GetTempPath()));

        Assert.True(executor.CanHandle(TweakType.Process));
        Assert.False(executor.CanHandle(TweakType.Registry));
        Assert.False(executor.CanHandle(TweakType.Service));
    }

    /// <summary>
    /// Test: PROC-02 (background process killing) kills all listed processes.
    /// </summary>
    [Fact]
    public async Task ProcessOperation_ApplyBackgroundProcesses_KillsAllListedProcesses()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var processManager = new FakeProcessManager();

        // Register several background processes from the Priority.ps1 kill list
        var killList = new[]
        {
            "steam", "EADesktop", "EpicGamesLauncher", "Battle.net",
            "BsgLauncher", "GalaxyClient", "RobloxPlayerBeta", "RiotClientServices",
            "Launcher", "upc",
        };

        foreach (var proc in killList)
        {
            processManager.RegisterProcess(proc);
        }

        var executor = new ProcessOperationExecutor(processManager, stateService, logService);

        var definition = new TweakDefinition
        {
            Id = "PROC-02",
            Name = "Background Process Management",
            Type = TweakType.Process,
            ProcessNames = killList.ToList(),
            RequiresRestart = false,
            RequiresAdmin = true,
            SortOrder = 2,
        };

        // Act
        var result = await executor.ApplyAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.Applied, result.Status);

        // All processes should be killed
        foreach (var proc in killList)
        {
            Assert.True(processManager.WasProcessKilled(proc), $"Process {proc} was not killed");
        }

        // Log should contain the operation
        var logContent = await File.ReadAllTextAsync(logService.LogFilePath);
        Assert.Contains("killed", logContent);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: Revert restores process priority to Normal (from High).
    /// </summary>
    [Fact]
    public async Task ProcessOperation_RevertRestoresNormalPriority()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var logService = new FileLogService(tempDir);
        var processManager = new FakeProcessManager();

        processManager.RegisterProcess("gam.exe");

        var executor = new ProcessOperationExecutor(processManager, stateService, logService);

        var definition = new TweakDefinition
        {
            Id = "PROC-01",
            Name = "Game Process Priority",
            Type = TweakType.Process,
            ProcessNames = new List<string> { "gam.exe" },
            ProcessPriority = "High",
            RequiresAdmin = true,
        };

        // Act — revert
        var result = await executor.RevertAsync(definition);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.NotApplied, result.Status);

        // Process priority should be restored to Normal
        var priority = processManager.GetPriority("gam.exe");
        Assert.Equal("Normal", priority);

        Cleanup(tempDir);
    }

    /// <summary>
    /// Test: GameProcessPriorityTweak definition has correct properties.
    /// </summary>
    [Fact]
    public void GameProcessPriorityTweak_HasCorrectProperties()
    {
        var def = GameProcessPriorityTweak.Definition;

        Assert.Equal("PROC-01", def.Id);
        Assert.Equal("Game Process Priority", def.Name);
        Assert.Equal("Process", def.Category);
        Assert.Equal(TweakType.Process, def.Type);
        Assert.NotNull(def.ProcessNames);
        Assert.Equal("High", def.ProcessPriority);
        Assert.True(def.RequiresAdmin);
        Assert.False(def.RequiresRestart);
        Assert.Equal(1, def.SortOrder);

        // ProcessNames should be empty (resolved at runtime by UI)
        Assert.Empty(def.ProcessNames!);
    }

    /// <summary>
    /// Test: BackgroundProcessesTweak definition has correct process list from Priority.ps1 line 80.
    /// </summary>
    [Fact]
    public void BackgroundProcessesTweak_HasCorrectProcessList()
    {
        var def = BackgroundProcessesTweak.Definition;

        Assert.Equal("PROC-02", def.Id);
        Assert.Equal("Background Process Management", def.Name);
        Assert.Equal("Process", def.Category);
        Assert.Equal(TweakType.Process, def.Type);
        Assert.NotNull(def.ProcessNames);

        // Verify from Priority.ps1 line 80: "Battle.net", "BsgLauncher", "EADesktop",
        // "EpicGamesLauncher", "GalaxyClient", "RobloxPlayerBeta", "RiotClientServices",
        // "Launcher", "steam", "upc"
        var expected = new[]
        {
            "Battle.net",
            "BsgLauncher",
            "EADesktop",
            "EpicGamesLauncher",
            "GalaxyClient",
            "RobloxPlayerBeta",
            "RiotClientServices",
            "Launcher",
            "steam",
            "upc",
        };

        foreach (var proc in expected)
        {
            Assert.Contains(proc, def.ProcessNames!);
        }

        Assert.True(def.RequiresAdmin);
        Assert.False(def.RequiresRestart);
        Assert.Equal(2, def.SortOrder);
    }

    /// <summary>
    /// Test: Apply returns failure when ProcessNames is missing (PROC-01 without priority).
    /// </summary>
    [Fact]
    public async Task ProcessOperation_ApplyWithMissingProcessNames_ReturnsFailure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var executor = new ProcessOperationExecutor(
            new FakeProcessManager(),
            new JsonFileStateService(new FakeRegistryProvider(),
                Path.Combine(tempDir, "state.json")),
            new FileLogService(tempDir));

        var definition = new TweakDefinition
        {
            Id = "BAD-PROC",
            Name = "Bad Process",
            Type = TweakType.Process,
            // No ProcessNames
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