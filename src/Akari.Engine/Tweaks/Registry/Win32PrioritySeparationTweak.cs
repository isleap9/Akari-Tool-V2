// REG-04: Win32PrioritySeparation tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SYSTEM\CurrentControlSet\Control\PriorityControl
// Value: Win32PrioritySeparation, DWORD, enabled=26 (0x26), disabled=38 (0x26 default)
//
// Note: The enabled value 26 (0x26) gives foreground apps higher priority and
// reduces priority for background tasks. The default/disabled value is 38.

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Win32PrioritySeparation (REG-04): Sets Win32PrioritySeparation to 26 (0x26)
/// for CPU priority optimization — gives foreground apps higher priority,
/// reducing priority for background tasks during gaming.
/// </summary>
public class Win32PrioritySeparationTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-04",
        Name = "Win32 Priority Separation",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Sets Win32PrioritySeparation=26 (0x26) to prioritize foreground apps and reduce background task CPU usage during gaming.",
        RegistryKey = @"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl",
        RegistryValueName = "Win32PrioritySeparation",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "26",  // 0x26 — foreground priority optimization
        RegistryRevertValueData = "38", // 0x26 default — standard priority
        RequiresRestart = true,
        RequiresAdmin = true,
        SortOrder = 4,
    };
}
