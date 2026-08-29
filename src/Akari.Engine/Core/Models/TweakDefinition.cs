// Tweak models for Akari Engine.
// Core data structures used by the tweak engine, executors, and state service.

using System.Text.Json.Serialization;

namespace Akari.Engine.Core.Models;

/// <summary>
/// Defines the type of a tweak, used for engine dispatch (Strategy pattern).
/// </summary>
public enum TweakType
{
    /// <summary>Windows registry value modification.</summary>
    Registry,

    /// <summary>Windows service start/stop configuration.</summary>
    Service,

    /// <summary>Process priority or affinity adjustment.</summary>
    Process,

    /// <summary>Power plan activation via powercfg.</summary>
    Power,

    /// <summary>Memory platform setting adjustment.</summary>
    Memory,
}

/// <summary>
/// Enumerated status of a tweak's current applied state.
/// </summary>
public enum TweakStatus
{
    /// <summary>The tweak has not been applied (or was reverted).</summary>
    NotApplied,

    /// <summary>The tweak is currently applied.</summary>
    Applied,
}

/// <summary>
/// Describes a single tweak definition from the JSON catalog.
/// The engine dispatches Apply/Revert to the ITweakExecutor that CanHandle this type.
/// </summary>
public class TweakDefinition
{
    /// <summary>Unique identifier for the tweak (e.g. "REG-01"). Maps to REQUIREMENTS.md.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name shown in the UI.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Category grouping for the modular checklist UI (e.g. "Registry", "Services").</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The type of tweak — determines which ITweakExecutor handles it (Strategy dispatch).</summary>
    public TweakType Type { get; set; } = TweakType.Registry;

    /// <summary>Full description of what this tweak does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Full registry key path (e.g. "HKLM:\SOFTWARE\Microsoft\..."). Only for TweakType.Registry.</summary>
    public string? RegistryKey { get; set; }

    /// <summary>Registry value name (e.g. "GameDVR_DXGIHonorFSEWindowsCompatible").</summary>
    public string? RegistryValueName { get; set; }

    /// <summary>Registry value data to write when applying the tweak, as a JSON element to support multiple types.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Microsoft.Win32.RegistryValueKind? RegistryValueKind { get; set; }

    /// <summary>The value to write when applying (serialized as JSON for type flexibility).</summary>
    public string? RegistryValueData { get; set; }

    /// <summary>The value to write when reverting (disabled/default value). If null, the value is deleted.</summary>
    public string? RegistryRevertValueData { get; set; }

    /// <summary>Multi-value registry entries (e.g. Mouse Acceleration: MouseSpeed, MouseThreshold1, MouseThreshold2).
    /// When populated, applies all values instead of the single RegistryValueName.</summary>
    public List<RegistryMultiValue>? RegistryMultiValues { get; set; }

    /// <summary>Services managed by this tweak (for TweakType.Service).
    /// Applies/disables and stops/starts these Windows services.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ServiceNames { get; set; }

    /// <summary>Processes targeted by this tweak (for TweakType.Process).
    /// Used for PROC-01 (process priority by name) and PROC-02 (kill list).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ProcessNames { get; set; }

    /// <summary>Process priority to set when applying (for TweakType.Process, PROC-01).
    /// Values: "Idle", "BelowNormal", "Normal", "AboveNormal", "High", "RealTime".</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProcessPriority { get; set; }

    /// <summary>Service start type value to write to registry when applying (default 4 = disabled).
    /// Service start registry value at HKLM\SYSTEM\CurrentControlSet\Services\service\Start.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceStartValue { get; set; }

    /// <summary>Service start type value to write when reverting (default 3 = manual).
    /// Service start registry value at HKLM\SYSTEM\CurrentControlSet\Services\service\Start.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceRevertStartValue { get; set; }

    /// <summary>Power scheme GUID to activate when applying (e.g. 99999999-9999-9999-9999-999999999999).
    /// Used by TweakType.Power tweaks (PWR-01, PWR-02).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PowerSchemeGuid { get; set; }

    /// <summary>Base power scheme GUID to duplicate (e.g. e9a42b02-d5df-448d-aa00-03f14749eb61 for Ultimate Performance).
    /// Used by PWR-01 to create a custom copy before activation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PowerBaseSchemeGuid { get; set; }

    /// <summary>PowerShell command to execute when applying (e.g. Disable-MMAgent -MemoryCompression).
    /// Used by TweakType.Memory tweaks (MEM-01).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PowerShellCommand { get; set; }

    /// <summary>PowerShell command to execute when reverting (e.g. Enable-MMAgent -MemoryCompression).
    /// Used by TweakType.Memory tweaks (MEM-01).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PowerShellRevertCommand { get; set; }

    /// <summary>Whether the tweak requires an application restart or system reboot to take effect.</summary>
    public bool RequiresRestart { get; set; }

    /// <summary>Whether the tweak requires admin elevation to apply.</summary>
    public bool RequiresAdmin { get; set; } = true;

    /// <summary>Display order within the category group in the UI.</summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Result of a single tweak apply or revert operation.
/// </summary>
public class TweakResult
{
    /// <summary>The tweak ID this result corresponds to.</summary>
    public string TweakId { get; init; } = string.Empty;

    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>The resulting status after the operation.</summary>
    public TweakStatus Status { get; init; }

    /// <summary>Error message if the operation failed, null otherwise.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>The actual registry value read back after the operation, for verification.</summary>
    public string? ActualValue { get; init; }

    /// <summary>Timestamp of when this result was produced.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}

/// <summary>
/// Represents a single value in a multi-value registry tweak (e.g. Mouse Acceleration).
/// </summary>
public class RegistryMultiValue
{
    /// <summary>Full registry key path for this value (e.g. "HKCU:\Control Panel\Desktop").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The value name (e.g. "MouseSpeed").</summary>
    public string ValueName { get; set; } = string.Empty;

    /// <summary>The value data as a string (parsed by the executor based on RegistryValueKind).</summary>
    public string ValueData { get; set; } = string.Empty;

    /// <summary>The registry value kind.</summary>
    public Microsoft.Win32.RegistryValueKind ValueKind { get; set; } = Microsoft.Win32.RegistryValueKind.DWord;
}
