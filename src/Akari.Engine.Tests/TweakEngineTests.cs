// TweakEngineTests — tests the Strategy-pattern dispatch in TweakEngine.
//
// FakeRegistryProvider tests are logic-only — ACL failures caught at runtime
// via elevated launch + log check (see PITFALLS.md Pitfall 2, D-04).

using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Registry;
using Akari.Engine.Storage;
using Microsoft.Win32;
using Xunit;

namespace Akari.Engine.Tests;

/// <summary>
/// Tests for the TweakEngine Strategy-pattern dispatch.
/// Uses FakeTweakExecutor and a FakeRegistryProvider-backed state service.
/// </summary>
public class TweakEngineTests
{
    /// <summary>
    /// Tracer test: engine dispatches ApplyAsync to the matching executor via Strategy pattern.
    /// </summary>
    [Fact]
    public async Task TweakEngine_ApplyAsync_DispatchesToMatchingExecutor()
    {
        // Arrange — use FileLogService with temp directory for isolation
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var catalog = new TestCatalog();
        var executor = new TestRegistryExecutor();

        var engine = new TweakEngine(
            catalog,
            new[] { (ITweakExecutor)executor },
            stateService,
            logService);

        // Act
        var result = await engine.ApplyAsync("TEST-01");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("TEST-01", result.TweakId);
        Assert.True(executor.WasCalled);
        await LogCleanup(tempDir);
    }

    /// <summary>
    /// Test: engine returns failure when no executor matches the tweak type.
    /// </summary>
    [Fact]
    public async Task TweakEngine_ApplyAsync_ReturnsFailureWhenNoExecutorMatches()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var catalog = new TestCatalog();

        // No executors registered
        var engine = new TweakEngine(
            catalog,
            Array.Empty<ITweakExecutor>(),
            stateService,
            logService);

        // Act
        var result = await engine.ApplyAsync("TEST-01");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No executor", result.ErrorMessage);
        await LogCleanup(tempDir);
    }

    /// <summary>
    /// Test: ApplyBatchAsync applies multiple tweaks and returns results in order.
    /// </summary>
    [Fact]
    public async Task TweakEngine_ApplyBatchAsync_ReturnsResultsInOrder()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var catalog = new TestCatalog();
        var executor = new TestRegistryExecutor();

        var engine = new TweakEngine(
            catalog,
            new[] { (ITweakExecutor)executor },
            stateService,
            logService);

        // Act
        var results = await engine.ApplyBatchAsync(new[] { "TEST-01", "TEST-02" });

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[0].Success);
        Assert.True(results[1].Success);
        await LogCleanup(tempDir);
    }

    /// <summary>
    /// Test: RevertAsync dispatches to the matching executor and updates state.
    /// </summary>
    [Fact]
    public async Task TweakEngine_RevertAsync_DispatchesToExecutor()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var catalog = new TestCatalog();
        var executor = new TestRegistryExecutor();

        var engine = new TweakEngine(
            catalog,
            new[] { (ITweakExecutor)executor },
            stateService,
            logService);

        // Act
        var result = await engine.RevertAsync("TEST-01");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(TweakStatus.NotApplied, result.Status);
        await LogCleanup(tempDir);
    }

    /// <summary>
    /// Test: GetStatusAsync returns persisted status.
    /// </summary>
    [Fact]
    public async Task TweakEngine_GetStatusAsync_ReturnsPersistedStatus()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logService = new FileLogService(tempDir);
        var registryProvider = new FakeRegistryProvider();
        var stateService = new JsonFileStateService(registryProvider,
            Path.Combine(tempDir, "state.json"));
        var catalog = new TestCatalog();
        var executor = new TestRegistryExecutor();

        var engine = new TweakEngine(
            catalog,
            new[] { (ITweakExecutor)executor },
            stateService,
            logService);

        // Set to Applied first
        await stateService.UpdateAsync("TEST-01", TweakStatus.Applied);

        // Act
        var status = await engine.GetStatusAsync("TEST-01");

        // Assert
        Assert.Equal(TweakStatus.Applied, status);
        await LogCleanup(tempDir);
    }

    private static async Task LogCleanup(string tempDir)
    {
        if (Directory.Exists(tempDir))
        {
            await Task.Run(() => Directory.Delete(tempDir, true));
        }
    }

    // --- Test doubles ---

    /// <summary>
    /// In-memory catalog for testing — returns predefined tweak definitions.
    /// </summary>
    private class TestCatalog : ITweakCatalog
    {
        private readonly List<TweakDefinition> _tweaks = new()
        {
            new TweakDefinition
            {
                Id = "TEST-01",
                Name = "Test Tweak 1",
                Category = "Registry",
                Type = TweakType.Registry,
                RegistryKey = "HKLM:\\SOFTWARE\\Akari\\Test",
                RegistryValueName = "TestValue",
                RegistryValueKind = RegistryValueKind.DWord,
                RegistryValueData = "42",
                RequiresRestart = false,
                RequiresAdmin = false,
            },
            new TweakDefinition
            {
                Id = "TEST-02",
                Name = "Test Tweak 2",
                Category = "Registry",
                Type = TweakType.Registry,
                RegistryKey = "HKLM:\\SOFTWARE\\Akari\\Test2",
                RegistryValueName = "TestValue2",
                RegistryValueKind = RegistryValueKind.DWord,
                RegistryValueData = "99",
                RequiresRestart = false,
                RequiresAdmin = false,
            },
        };

        public Task<TweakDefinition?> GetByIdAsync(string id) =>
            Task.FromResult(_tweaks.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<TweakDefinition>> GetAllAsync() =>
            Task.FromResult((IReadOnlyList<TweakDefinition>)_tweaks);

        public Task<IReadOnlyList<TweakDefinition>> GetByCategoryAsync(string category) =>
            Task.FromResult((IReadOnlyList<TweakDefinition>)_tweaks.Where(t => t.Category == category).ToList());
    }

    /// <summary>
    /// Test executor that handles TweakType.Registry and returns success.
    /// </summary>
    private class TestRegistryExecutor : ITweakExecutor
    {
        public bool WasCalled { get; private set; }

        public bool CanHandle(TweakType type) => type == TweakType.Registry;

        public Task<TweakResult> ApplyAsync(TweakDefinition definition)
        {
            WasCalled = true;
            return Task.FromResult(new TweakResult
            {
                TweakId = definition.Id,
                Success = true,
                Status = TweakStatus.Applied,
            });
        }

        public Task<TweakResult> RevertAsync(TweakDefinition definition)
        {
            WasCalled = true;
            return Task.FromResult(new TweakResult
            {
                TweakId = definition.Id,
                Success = true,
                Status = TweakStatus.NotApplied,
            });
        }
    }
}
