// PWR-01: Ultimate Performance power plan tweak.
// Activates the Ultimate Performance power scheme for maximum gaming performance.
//
// From AkariOS Tweaks/6 Windows/29 Power Plan.ps1 lines 21-25:
//   powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 99999999-9999-9999-9999-999999999999
//   powercfg /SETACTIVE 99999999-9999-9999-9999-999999999999
//
// PWR-02 (High Performance fallback) is handled automatically by the executor
// when Ultimate Performance is not available (Pitfall 9 — GUID confusion).

using Akari.Engine.Core.Models;

namespace Akari.Engine.Tweaks.Power;

/// <summary>
/// Ultimate Performance (PWR-01): Activates the Ultimate Performance power scheme
/// for maximum gaming performance. If the Ultimate Performance base scheme is not
/// available, falls back to High Performance plan (PWR-02/Pitfall 9).
/// </summary>
public class UltimatePerformanceTweak
{
    /// <summary>The Ultimate Performance base scheme GUID (source for duplication).</summary>
    public const string BaseSchemeGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    /// <summary>The target scheme GUID created by duplicating Ultimate Performance.</summary>
    public const string TargetSchemeGuid = "99999999-9999-9999-9999-999999999999";

    public static TweakDefinition Definition => new()
    {
        Id = "PWR-01",
        Name = "Ultimate Performance",
        Category = "Power",
        Type = TweakType.Power,
        Description = "Activates the Ultimate Performance power scheme for maximum gaming performance. " +
                      "Duplicates the Ultimate Performance base scheme and sets it as active. " +
                      "Falls back to High Performance if Ultimate Performance is not available.",
        PowerSchemeGuid = TargetSchemeGuid,
        PowerBaseSchemeGuid = BaseSchemeGuid,
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 1,
    };
}
