// REG-02: HAGS (Hardware-Accelerated GPU Scheduling) tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SYSTEM\CurrentControlSet\Control\GraphicsDrivers
// Value: HwSchMode, DWORD, enabled=2, disabled=1
//
// CRITICAL (Pitfall 3): This writes to the 64-bit registry view via RegistryView.Registry64.
// Without this, 32-bit processes would be redirected to Wow6432Node.

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// HAGS (REG-02): Toggles Hardware-Accelerated GPU Scheduling.
/// Writes to HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode
/// in the 64-bit registry view (NOT Wow6432Node — Pitfall 3, D-02).
/// </summary>
public class HagsTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-02",
        Name = "HAGS (Hardware-Accelerated GPU Scheduling)",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Enables hardware-accelerated GPU scheduling for lower latency and reduced stutter.",
        RegistryKey = @"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
        RegistryValueName = "HwSchMode",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "2",  // HwSchMode = 2 (enabled)
        RegistryRevertValueData = "1", // HwSchMode = 1 (disabled, default)
        RequiresRestart = true, // Requires full system restart
        RequiresAdmin = true,
        SortOrder = 2,
    };
}
