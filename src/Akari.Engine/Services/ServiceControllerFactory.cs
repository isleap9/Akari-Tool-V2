// ServiceControllerFactory — production implementation wrapping System.ServiceController.
//
// Uses System.ServiceController for runtime service management. All operations are
// async Task with Task.Run offloading (Pitfall 5 — D-11) to prevent UI thread blocking.
// This is the real implementation; FakeServiceControllerFactory is used for unit tests.

using System.ServiceProcess;

namespace Akari.Engine.Services;

/// <summary>
/// Production implementation of <see cref="IServiceControllerFactory"/> that wraps
/// <see cref="System.ServiceController"/> for real Windows service management.
/// All operations are async Task with Task.Run offloading to prevent UI blocking (D-11).
/// </summary>
public class ServiceControllerFactory : IServiceControllerFactory
{
    /// <summary>
    /// Gets a controller for the named service. Returns null if the service
    /// does not exist on this system.
    /// </summary>
    public async Task<ISystemServiceController?> GetServiceControllerAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            try
            {
                // First check if the service exists by querying the ServiceController
                var controller = new ServiceController(serviceName);
                return new ProductionServiceController(controller);
            }
            catch (InvalidOperationException)
            {
                // Service does not exist on this system
                return null;
            }
        });
    }

    /// <summary>
    /// Returns the names of services that depend on the given service.
    /// </summary>
    public async Task<IEnumerable<string>> GetDependentServicesAsync(string serviceName)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var controller = new ServiceController(serviceName);
                return controller.DependentServices
                    .Select(s => s.ServiceName)
                    .ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        });
    }
}

/// <summary>
/// Production implementation of <see cref="ISystemServiceController"/> wrapping
/// <see cref="System.ServiceController"/>.
/// </summary>
internal class ProductionServiceController : ISystemServiceController, IDisposable
{
    private readonly ServiceController _controller;

    public ProductionServiceController(ServiceController controller)
    {
        _controller = controller;
    }

    public string ServiceName => _controller.ServiceName;

    public bool IsRunning => _controller.Status == ServiceControllerStatus.Running;

    public ServiceStartType StartType
    {
        get
        {
            // Read from registry: HKLM\SYSTEM\CurrentControlSet\Services\<name>\Start
            // Uses the same 2-arg OpenSubKey pattern from Phase 1 (D-01)
            var keyPath = $@"HKLM:\SYSTEM\CurrentControlSet\Services\{_controller.ServiceName}";
            try
            {
                using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + _controller.ServiceName, true);
                if (key != null)
                {
                    var startValue = key.GetValue("Start") as int? ?? 3;
                    return (ServiceStartType)startValue;
                }
            }
            catch
            {
                // If we can't read the registry, assume manual start
            }
            return ServiceStartType.Manual;
        }
        set
        {
            var keyPath = $@"HKLM:\SYSTEM\CurrentControlSet\Services\{_controller.ServiceName}";
            try
            {
                using var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + _controller.ServiceName, true);
                if (key != null)
                {
                    key.SetValue("Start", (int)value, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch
            {
                // Log and swallow — service may not have a registry key
            }
        }
    }

    public async Task<bool> StopAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (_controller.Status == ServiceControllerStatus.Running)
                {
                    _controller.Stop();
                    _controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    return true;
                }
                return true; // Already stopped
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> StartAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (_controller.Status != ServiceControllerStatus.Running)
                {
                    _controller.Start();
                    _controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                    return true;
                }
                return true; // Already running
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<IEnumerable<string>> GetDependentServicesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                return _controller.DependentServices
                    .Select(s => s.ServiceName)
                    .ToArray();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        });
    }

    public void Dispose() => _controller?.Dispose();
}
