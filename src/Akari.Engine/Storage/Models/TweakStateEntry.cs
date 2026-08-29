// TweakStateEntry — a single entry in the persisted JSON state file.
// Stored as a dictionary of { tweakId → TweakStateEntry } in state.json.

using Akari.Engine.Core.Models;

namespace Akari.Engine.Storage.Models;

/// <summary>
/// A single tweak's persisted state entry in the JSON state file.
/// </summary>
public class TweakStateEntry
{
    /// <summary>The current status of this tweak (NotApplied or Applied).</summary>
    public TweakStatus Status { get; set; }

    /// <summary>UTC timestamp when the tweak was last applied.</summary>
    public DateTimeOffset? LastAppliedAt { get; set; }

    /// <summary>UTC timestamp when the tweak was last reverted.</summary>
    public DateTimeOffset? LastRevertedAt { get; set; }

    /// <summary>The expected value after applying the tweak (for re-validation).</summary>
    public string? ExpectedValue { get; set; }

    /// <summary>The actual value read from the registry during last verification.</summary>
    public string? CurrentValue { get; set; }
}
