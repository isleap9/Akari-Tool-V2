// IServiceControllerFactory — abstracts System.ServiceController for testability.
//
// The production ServiceControllerFactory wraps System.ServiceController.
// The FakeServiceControllerFactory is an in-memory implementation for unit tests.
// This abstraction is necessary because ServiceController requires admin rights
// and real Windows services — cannot be tested in unit tests without it.

namespace Akari.Engine.Services;

/// <summary>
/// Represents a Windows service that can be stopped, started, and have its
/// start type (registry) read/modified. This is the testable abstraction
/// over <see cref="System.ServiceController"/>.
/// </summary>
public interface ISystemServiceController
{
    /// <summary>The service name (e.g. "XblAuthManager").</summary>
    string ServiceName { get; }

    /// <summary>Whether the service is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>The current start type (Disabled, Manual, Auto, etc.).</summary>
    ServiceStartType StartType { get; set; }

    /// <summary>Asynchronously stops the service.</summary>
    Task<bool> StopAsync();

    /// <summary>Asynchronously starts the service.</summary>
    Task<bool> StartAsync();

    /// <summary>Returns the names of services that depend on this service.</summary>
    Task<IEnumerable<string>> GetDependentServicesAsync();
}

/// <summary>
/// Start type for a Windows service (maps to registry Start DWORD value at
/// HKLM\SYSTEM\CurrentControlSet\Services\name\Start). Values match the Windows
/// registry Start DWORD convention: 0=Boot, 1=System, 2=Auto, 3=Manual, 4=Disabled.
/// Per AkariOS Tweaks/8 Advanced/17 Services.ps1: disabled=Start=4, restore=Start=3.
/// </summary>
public enum ServiceStartType
{
    /// <summary>Boot (0) — loaded by the boot loader.</summary>
    Boot = 0,

    /// <summary>System (1) — loaded by the kernel.</summary>
    System = 1,

    /// <summary>Automatic (2) — started by the Service Control Manager at boot.</summary>
    Automatic = 2,

    /// <summary>Manual (3) — user or program starts it on demand. Maps to registry Start=3.</summary>
    Manual = 3,

    /// <summary>Disabled (4) — will not start. Maps to registry Start=4.</summary>
    Disabled = 4,
}

/// <summary>
/// Factory interface for creating <see cref="ISystemServiceController"/> instances.
/// Allows injecting a fake for unit testing (production uses real ServiceController).
/// </summary>
public interface IServiceControllerFactory
{
    /// <summary>
    /// Creates a controller for the named service, or returns null if the service
    /// does not exist on this system.
    /// </summary>
    Task<ISystemServiceController?> GetServiceControllerAsync(string serviceName);

    /// <summary>
    /// Returns the names of services that depend on the given service.
    /// Used for dependency-chain checking before stopping services (Pitfall 10).
    /// </summary>
    Task<IEnumerable<string>> GetDependentServicesAsync(string serviceName);
}
