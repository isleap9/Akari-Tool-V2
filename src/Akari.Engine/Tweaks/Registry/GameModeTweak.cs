// REG-01: Game Mode tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList
// Value: GameMode, DWORD, enabled=1, disabled=0 (default)

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Game Mode (REG-01): Toggles the Windows Game Mode registry value.
/// Game Mode optimizes the system for gaming by prioritizing game processes.
/// </summary>
public class GameModeTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-01",
        Name = "Game Mode",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Enables Windows Game Mode for optimal gaming performance by prioritizing game processes.",
        RegistryKey = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList",
        RegistryValueName = "GameMode",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "1",           // GameMode = 1 (enabled)
        RegistryRevertValueData = "0",     // GameMode = 0 (disabled, default)
        RequiresRestart = true,
        RequiresAdmin = true,
        SortOrder = 1,
    };
}
