// PWR-02: High Performance power plan tweak.
// Activates the High Performance power plan (used as fallback when Ultimate Performance
// is not available).
//
// From AkariOS Tweaks/6 Windows/29 Power Plan.ps1 revert path (line 240):
//   powercfg -restoredefaultschemes (restores all defaults including High Performance)
//
// High Performance GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
// This is the standard Windows High Performance power scheme GUID.

using Akari.Engine.Core.Models;

namespace Akari.Engine.Tweaks.Power;

/// <summary>
/// High Performance (PWR-02): Activates the High Performance power scheme as a
/// fallback when Ultimate Performance (PWR-01) is not available (Pitfall 9).
/// Also used as a standalone power plan activation for systems that don't support
/// Ultimate Performance.
/// </summary>
public class HighPerformanceTweak
{
    /// <summary>The standard Windows High Performance power scheme GUID.</summary>
    public const string SchemeGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    public static TweakDefinition Definition => new()
    {
        Id = "PWR-02",
        Name = "High Performance (Fallback)",
        Category = "Power",
        Type = TweakType.Power,
        Description = "Activates the High Performance power scheme. Used as a fallback when " +
                      "Ultimate Performance is not available, or as a standalone high-performance plan.",
        PowerSchemeGuid = SchemeGuid,
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 2,
    };
}
