// REG-05: Multimedia Tasks tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games
// Value: Priority, DWORD, enabled=6 (High), disabled=1 (Default)

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Multimedia Tasks (REG-05): Sets the Games multimedia task Priority to 6 (High)
/// so that multimedia system profile gives games higher scheduling priority.
/// </summary>
public class MultimediaTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-05",
        Name = "Multimedia System Profile Priority",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Sets the Games multimedia task Priority to 6 (High) for elevated scheduling priority during multimedia playback.",
        RegistryKey = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
        RegistryValueName = "Priority",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "6",  // High priority
        RegistryRevertValueData = "1", // Default priority
        RequiresRestart = true,
        RequiresAdmin = true,
        SortOrder = 5,
    };
}
