// PROC-02: Background Processes tweak.
// Kills game launcher and background processes during gaming to free system resources.
//
// Process kill list from AkariOS Tweaks/8 Advanced/10 Priority.ps1 line 80:
// "Battle.net", "BsgLauncher", "EADesktop", "EpicGamesLauncher", "GalaxyClient",
// "RobloxPlayerBeta", "RiotClientServices", "Launcher", "steam", "upcwpl"
//
// Apply: Kill all processes in the list
// Revert: No-op (processes must be restarted manually by the user or their launcher)

using Akari.Engine.Core.Models;

namespace Akari.Engine.Tweaks.Process;

/// <summary>
/// Background Processes (PROC-02): Stops game launcher and background processes
/// during gaming to free CPU, memory, and I/O resources. Process list is sourced from
/// the AkariOS Tweaks PowerShell scripts (Priority.ps1 line 80).
/// </summary>
public class BackgroundProcessesTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "PROC-02",
        Name = "Background Process Management",
        Category = "Process",
        Type = TweakType.Process,
        Description = "Stops game launcher and background processes during gaming to free " +
                      "system resources: Battle.net, BsgLauncher, EADesktop, EpicGamesLauncher, " +
                      "GalaxyClient, RobloxPlayerBeta, RiotClientServices, Launcher, steam, upcwpl.",
        ProcessNames = new List<string>
        {
            "Battle.net",
            "BsgLauncher",
            "EADesktop",
            "EpicGamesLauncher",
            "GalaxyClient",
            "RobloxPlayerBeta",
            "RiotClientServices",
            "Launcher",
            "steam",
            "upc",
        },
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 2,
    };
}