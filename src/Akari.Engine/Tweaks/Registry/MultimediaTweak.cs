// REG-05: Multimedia Tasks tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games
// Values: GPU Priority=8, Priority=6, Scheduling Category=High (multiple values)
//
// Uses RegistryMultiValues for the 3 values (GPU Priority, Priority, Scheduling Category).

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Multimedia Tasks (REG-05): Sets the Games multimedia task Priority=6 (High),
/// GPU Priority=8 (High), and Scheduling Category=High for elevated scheduling
/// priority during multimedia playback and gaming.
/// </summary>
public class MultimediaTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-05",
        Name = "Multimedia System Profile Priority",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Sets the Games multimedia task Priority=6 (High), GPU Priority=8 (High), and Scheduling Category=High for elevated scheduling priority during gaming.",
        RegistryKey = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
        RegistryValueName = "Priority",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "6",  // High priority
        RegistryRevertValueData = "1", // Default priority

        // Multi-value: GPU Priority, Priority, Scheduling Category
        RegistryMultiValues = new List<RegistryMultiValue>
        {
            new RegistryMultiValue
            {
                Key = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                ValueName = "GPU Priority",
                ValueData = "8",
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                ValueName = "Priority",
                ValueData = "6",
                ValueKind = RegistryValueKind.DWord,
            },
            new RegistryMultiValue
            {
                Key = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                ValueName = "Scheduling Category",
                ValueData = "High",
                ValueKind = RegistryValueKind.String,
            },
        },

        RequiresRestart = true,
        RequiresAdmin = true,
        SortOrder = 5,
    };
}
