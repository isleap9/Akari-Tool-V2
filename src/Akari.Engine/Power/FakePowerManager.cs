// FakePowerManager — in-memory implementation for unit tests.
//
// CRITICAL (D-04, PITFALLS.md Pitfall 2):
// This manager does NOT interact with real Windows power subsystem. It is an in-memory
// dictionary with NO real powercfg execution. Unit tests using this fake validate
// LOGIC ONLY. Runtime verification of actual power operations must be performed
// via an elevated app launch and log file inspection at
// %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.

using Akari.Engine.Registry;
using Microsoft.Win32;

namespace Akari.Engine.Power;

/// <summary>
/// In-memory implementation of <see cref="IPowerManager"/> for unit tests.
/// Does NOT interact with real Windows power subsystem — logic-only validation
/// (per D-04, PITFALLS.md Pitfall 2). Runtime power operations require an actual
/// elevated launch + log check.
/// </summary>
public class FakePowerManager : IPowerManager
{
    private readonly IRegistryProvider _registry;
    private readonly Dictionary<string, PowerSchemeInfo> _schemes = new();
    private string? _activeSchemeGuid;
    private bool _hibernateEnabled = true;
    private bool _restoreDefaultsCalled;

    public FakePowerManager(IRegistryProvider registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Pre-registers a power scheme in the fake store for testing.
    /// </summary>
    public void RegisterScheme(string guid, string name, bool isActive = false)
    {
        _schemes[guid] = new PowerSchemeInfo { Guid = guid, Name = name, IsActive = isActive };
        if (isActive)
        {
            _activeSchemeGuid = guid;
        }
    }

    /// <inheritdoc/>
    public Task<PowerOperationResult> SetActiveSchemeAsync(string schemeGuid)
    {
        if (_schemes.ContainsKey(schemeGuid))
        {
            _activeSchemeGuid = schemeGuid;
            return Task.FromResult(new PowerOperationResult
            {
                Success = true,
                Output = $"Power scheme {schemeGuid} set as active."
            });
        }

        return Task.FromResult(new PowerOperationResult
        {
            Success = false,
            ErrorMessage = $"Power scheme {schemeGuid} not found."
        });
    }

    /// <inheritdoc/>
    public Task<PowerOperationResult> DuplicateSchemeAsync(string baseGuid, string targetGuid)
    {
        if (_schemes.ContainsKey(baseGuid))
        {
            var source = _schemes[baseGuid];
            _schemes[targetGuid] = new PowerSchemeInfo
            {
                Guid = targetGuid,
                Name = $"Copy of {source.Name}",
                IsActive = false
            };
            return Task.FromResult(new PowerOperationResult
            {
                Success = true,
                Output = $"Scheme {targetGuid} created from {baseGuid}."
            });
        }

        return Task.FromResult(new PowerOperationResult
        {
            Success = false,
            ErrorMessage = $"Base scheme {baseGuid} not found."
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<PowerSchemeInfo>> ListSchemesAsync()
    {
        var result = _schemes.Values.Select(s => new PowerSchemeInfo
        {
            Guid = s.Guid,
            Name = s.Name,
            IsActive = s.Guid == _activeSchemeGuid
        }).ToList();

        return Task.FromResult<IEnumerable<PowerSchemeInfo>>(result);
    }

    /// <inheritdoc/>
    public Task<PowerOperationResult> RestoreDefaultSchemesAsync()
    {
        _schemes.Clear();
        _activeSchemeGuid = null;
        _restoreDefaultsCalled = true;
        return Task.FromResult(new PowerOperationResult
        {
            Success = true,
            Output = "Default power schemes restored."
        });
    }

    /// <inheritdoc/>
    public Task<PowerOperationResult> SetHibernateAsync(bool enabled)
    {
        _hibernateEnabled = enabled;
        return Task.FromResult(new PowerOperationResult
        {
            Success = true,
            Output = $"Hibernate {(enabled ? "enabled" : "disabled")}."
        });
    }

    /// <inheritdoc/>
    public async Task<bool> SetRegistryValueAsync(string keyPath, string valueName, int value)
    {
        await _registry.SetValueAsync(keyPath, valueName, value, RegistryValueKind.DWord);
        return true;
    }

    /// <summary>
    /// Verifies that SetActiveSchemeAsync was called with the given GUID.
    /// </summary>
    public bool WasSchemeActivated(string? schemeGuid) => _activeSchemeGuid == schemeGuid;

    /// <summary>
    /// Verifies that RestoreDefaultSchemesAsync was called.
    /// </summary>
    public bool WasRestoreDefaultsCalled => _restoreDefaultsCalled;

    /// <summary>
    /// Gets the current active scheme GUID.
    /// </summary>
    public string? ActiveSchemeGuid => _activeSchemeGuid;
}