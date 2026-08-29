// PROC-01: Game Process Priority tweak.
// Sets the active game process to High priority for improved gaming performance.
//
// From AkariOS Tweaks/8 Advanced/10 Priority.ps1 §"1. Already Running" (line 51):
// Shows processes with WorkingSet64 > 500MB, lets user select by ID, sets priority.
// Priority options: RealTime, High, AboveNormal, Normal, BelowNormal, Idle
//
// In Akari Tool V2, the process name is resolved at runtime by the UI (Phase 4).
// The definition carries ProcessPriority="High" and an empty ProcessNames list
// (to be populated at runtime when the user selects a process).
// For unit testing, ProcessNames is set to a test value.

using Akari.Engine.Core.Models;

namespace Akari.Engine.Tweaks.Process;

/// <summary>
/// Game Process Priority (PROC-01): Sets the active game process to High priority
/// for improved gaming performance. The target process is selected at runtime by the
/// user from the list of running processes with WorkingSet64 > 500MB (matching the
/// PowerShell reference in Priority.ps1).
/// </summary>
public class GameProcessPriorityTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "PROC-01",
        Name = "Game Process Priority",
        Category = "Process",
        Type = TweakType.Process,
        Description = "Sets the active game process to High priority to prioritize" +
                      " CPU scheduling for gaming. Process is selected at runtime from running" +
                      " processes with WorkingSet64 > 500MB (matching Priority.ps1 pattern).",
        // ProcessNames is empty — resolved at runtime by the UI when user selects a process
        ProcessNames = new List<string>(),
        ProcessPriority = "High",
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 1,
    };
}