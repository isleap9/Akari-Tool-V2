// REG-06: Visual Effects tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects
// Values: VisualFXSetting=2 (master switch) + individual effect toggles
//
// Uses RegistryMultiValues for multiple visual effect settings:
// - VisualFXSetting: Master switch (2 = adjust for best performance)
// - AllGraphicsItems: Disable all graphical effects
// - Combo: Disable combo animation
// - Fade: Disable fade effects
// - Drag: Disable drag animation
// - Menu: Disable menu animation
// - Select: Disable selection fade

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Visual Effects (REG-06): Disables visual animations, transparency, and shadows
/// to reduce GPU/CPU overhead during gaming. Uses both the master VisualFXSetting
/// switch (2 = adjust for best performance) and individual effect toggles.
/// </summary>
public class VisualEffectsTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-06",
        Name = "Visual Effects Optimization",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Disables visual animations, transparency, and shadows (VisualFXSetting=2 plus individual effect toggles) to reduce GPU/CPU overhead during gaming.",
        RegistryKey = @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
        RegistryValueName = "VisualFXSetting",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "2",  // Adjust for best performance
        RegistryRevertValueData = "3", // Default (let Windows choose)

        // Multi-value: Master switch + individual visual effect toggles
        RegistryMultiValues = new List<RegistryMultiValue>
        {
            new RegistryMultiValue
            {
                Key = @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                ValueName = "VisualFXSetting",
                ValueData = "2",
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKEY_USERS\.DEFAULT\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                ValueName = "VisualFXSetting",
                ValueData = "2",
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                ValueName = "VisualFXSetting",
                ValueData = "2",
                ValueKind = RegistryValueKind.DWord,
            },
            // Disable specific visual effects via UXTheme
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\AnimateMinMax",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\Combo",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\Fade",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\Drag",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\Menu",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\Select",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects\TaskbarList",
                ValueName = "Default",
                ValueData = "1",  // 1 = disabled
                ValueKind = RegistryValueKind.DWord,
            },
        },

        RequiresRestart = true,
        RequiresAdmin = true,
        SortOrder = 6,
    };
}
