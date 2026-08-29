// FakeServiceControllerFactory — in-memory implementation for unit tests.
//
// CRITICAL (D-04, PITFALLS.md Pitfall 2):
// This factory does NOT interact with real Windows services. It is an in-memory
// dictionary with NO ACL enforcement and NO real service control. Unit tests using
// this fake validate LOGIC ONLY. Runtime verification of actual service operations
// must be performed via an elevated app launch and log file inspection at
// %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.

using System.Collections.Concurrent;

namespace Akari.Engine.Services;

/// <summary>
/// In-memory implementation of <see cref="IServiceControllerFactory"/> for unit tests.
/// Does NOT interact with real Windows services — logic-only validation (per D-04,
/// PITFALLS.md Pitfall 2). Runtime service operations require an actual elevated
/// launch + log check.
/// </summary>
public class FakeServiceControllerFactory : IServiceControllerFactory
{
    private readonly ConcurrentDictionary<string, FakeServiceController> _services = new();

    /// <summary>
    /// Registers a fake service with the given name and initial state.
    /// </summary>
    public void RegisterService(string serviceName, bool isRunning = true, ServiceStartType startType = ServiceStartType.Automatic)
    {
        _services[serviceName] = new FakeServiceController(serviceName, isRunning, startType);
    }

    /// <inheritdoc/>
    public Task<ISystemServiceController?> GetServiceControllerAsync(string serviceName)
    {
        _services.TryGetValue(serviceName, out var controller);
        return Task.FromResult<ISystemServiceController?>(controller);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> GetDependentServicesAsync(string serviceName)
    {
        if (_services.TryGetValue(serviceName, out var controller))
        {
            return Task.FromResult(controller.GetDependentServices());
        }
        return Task.FromResult(Enumerable.Empty<string>());
    }

    /// <summary>
    /// Returns the number of service operations (Stop/Start) performed on fake services.
    /// Useful for assertions in tests.
    /// </summary>
    public int TotalOperations => _services.Values.Sum(s => s.StopCount + s.StartCount);

    /// <summary>
    /// Verifies that a service was stopped.
    /// </summary>
    public bool WasServiceStopped(string serviceName) =>
        _services.TryGetValue(serviceName, out var controller) && controller.StopCount > 0;

    /// <summary>
    /// Verifies that a service was started.
    /// </summary>
    public bool WasServiceStarted(string serviceName) =>
        _services.TryGetValue(serviceName, out var controller) && controller.StartCount > 0;

    /// <summary>
    /// Gets the current start type for a fake service.
    /// </summary>
    public ServiceStartType? GetStartType(string serviceName) =>
        _services.TryGetValue(serviceName, out var controller) ? controller.StartType : null;
}

/// <summary>
/// In-memory fake service controller for unit testing.
/// </summary>
internal class FakeServiceController : ISystemServiceController
{
    public string ServiceName { get; }
    public bool IsRunning { get; private set; }
    public ServiceStartType StartType { get; set; }
    public int StopCount { get; private set; }
    public int StartCount { get; private set; }
    private readonly List<string> _dependents = new();

    public FakeServiceController(string serviceName, bool isRunning, ServiceStartType startType)
    {
        ServiceName = serviceName;
        IsRunning = isRunning;
        StartType = startType;
    }

    public Task<bool> StopAsync()
    {
        IsRunning = false;
        StopCount++;
        return Task.FromResult(true);
    }

    public Task<bool> StartAsync()
    {
        IsRunning = true;
        StartCount++;
        return Task.FromResult(true);
    }

    public Task<IEnumerable<string>> GetDependentServicesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_dependents);
    }

    internal IEnumerable<string> GetDependentServices() => _dependents;

    internal void AddDependent(string serviceName) => _dependents.Add(serviceName);
}
