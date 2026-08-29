// REG-07: Mouse Acceleration tweak.
// Hive: HKEY_CURRENT_USER (NOT HKLM — per RESEARCH.md)
// Key: Control Panel\Desktop
// Values: MouseSpeed=0, MouseThreshold1=0, MouseThreshold2=0 (multiple values)
//
// CRITICAL: This uses HKCU, not HKLM. HKCU does NOT require admin elevation.
// This is the only registry tweak in HKLM... wait, HKCU requires no admin.
// Uses RegistryMultiValues for the 3 values (MouseSpeed, MouseThreshold1, MouseThreshold2).

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Mouse Acceleration (REG-07): Disables mouse acceleration by setting
/// MouseSpeed=0, MouseThreshold1=0, MouseThreshold2=0 in
/// HKCU\Control Panel\Desktop (per RESEARCH.md — HKCU, not HKLM).
/// </summary>
public class MouseAccelerationTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-07",
        Name = "Disable Mouse Acceleration",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Disables mouse acceleration for consistent, precise cursor movement during gaming.",
        // HKCU does NOT require admin elevation
        RequiresAdmin = false,
        RequiresRestart = false,
        SortOrder = 7,

        RegistryKey = @"HKCU:\Control Panel\Desktop",
        RegistryValueName = "MouseSpeed",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "0",           // MouseSpeed = 0 (acceleration disabled)
        RegistryRevertValueData = "2",     // MouseSpeed = 2 (default: enhanced pointer precision on)

        // Multi-value: MouseSpeed, MouseThreshold1, MouseThreshold2
        RegistryMultiValues = new List<RegistryMultiValue>
        {
            new RegistryMultiValue
            {
                Key = @"HKCU:\Control Panel\Desktop",
                ValueName = "MouseSpeed",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Control Panel\Desktop",
                ValueName = "MouseThreshold1",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Control Panel\Desktop",
                ValueName = "MouseThreshold2",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
        },
    };
}
