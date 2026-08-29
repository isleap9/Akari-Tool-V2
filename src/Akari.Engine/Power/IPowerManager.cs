// IPowerManager — abstracts powercfg.exe for testability.
//
// The production PowerManager wraps powercfg.exe via Process.Start.
// The FakePowerManager is an in-memory implementation for unit tests.
// This abstraction is necessary because powercfg requires admin rights
// and real Windows power subsystem — cannot be tested in unit tests without it.

namespace Akari.Engine.Power;

/// <summary>
/// Represents a Windows power scheme.
/// </summary>
public class PowerSchemeInfo
{
    /// <summary>The GUID of the power scheme.</summary>
    public string Guid { get; init; } = string.Empty;

    /// <summary>The display name of the power scheme.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Whether this scheme is currently active.</summary>
    public bool IsActive { get; init; }
}

/// <summary>
/// Result of a power operation.
/// </summary>
public class PowerOperationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Output from the powercfg command (if any).</summary>
    public string? Output { get; init; }

    /// <summary>Error message if the operation failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Abstraction over <c>powercfg.exe</c> for Windows power plan management.
/// Allows injecting a fake for unit testing (production uses real powercfg.exe).
/// </summary>
public interface IPowerManager
{
    /// <summary>
    /// Sets the active power scheme by GUID.
    /// Corresponds to: powercfg /SETACTIVE &lt;guid&gt;
    /// </summary>
    Task<PowerOperationResult> SetActiveSchemeAsync(string schemeGuid);

    /// <summary>
    /// Duplicates a power scheme. Creates a copy of the base scheme with a new GUID.
    /// Corresponds to: powercfg /duplicatescheme &lt;baseGuid&gt; &lt;targetGuid&gt;
    /// </summary>
    Task<PowerOperationResult> DuplicateSchemeAsync(string baseGuid, string targetGuid);

    /// <summary>
    /// Lists all available power schemes.
    /// Corresponds to: powercfg /LIST
    /// </summary>
    Task<IEnumerable<PowerSchemeInfo>> ListSchemesAsync();

    /// <summary>
    /// Restores all default power schemes.
    /// Corresponds to: powercfg -restoredefaultschemes
    /// </summary>
    Task<PowerOperationResult> RestoreDefaultSchemesAsync();

    /// <summary>
    /// Enables or disables hibernation.
    /// Corresponds to: powercfg /hibernate on|off
    /// </summary>
    Task<PowerOperationResult> SetHibernateAsync(bool enabled);

    /// <summary>
    /// Writes a registry value related to power settings.
    /// Uses the same 2-arg OpenSubKey pattern from Phase 1 (D-01, Pitfall 1).
    /// </summary>
    Task<bool> SetRegistryValueAsync(string keyPath, string valueName, int value);
}
