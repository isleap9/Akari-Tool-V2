// REG-06: Visual Effects tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects
// Value: VisualFXSetting, DWORD, enabled=2 (disable animations), disabled=3 (default)

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Visual Effects (REG-06): Sets VisualFXSetting to 2 (disable animations/transparency)
/// to reduce GPU/CPU overhead from Windows visual effects during gaming.
/// </summary>
public class VisualEffectsTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-06",
        Name = "Visual Effects Optimization",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Disables visual animations and transparency (VisualFXSetting=2) to reduce GPU/CPU overhead during gaming.",
        RegistryKey = @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
        RegistryValueName = "VisualFXSetting",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "2",  // Disable animations (performance optimized)
        RegistryRevertValueData = "3", // Default (let Windows choose)
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 6,
    };
}
