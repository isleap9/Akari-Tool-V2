// ITweakStateService — persists tweak status and detects Windows Update reverts.
//
// State persisted to %LOCALAPPDATA%\Akari\App\state.json as JSON (per D-03, ENG-06).
// The RevalidateAsync method (startup re-validation, D-05) reads each persisted
// "Applied" tweak's actual registry value and compares against the expected value.
// If they differ, the tweak status is reset to "NotApplied" — this detects
// Windows Update reverts that silently undo registry tweaks.

using Akari.Engine.Core.Models;

namespace Akari.Engine.Storage;

/// <summary>
/// Persists tweak status to JSON at
/// <c>%LOCALAPPDATA%\Akari\App\state.json</c> (per D-03, ENG-06).
/// Performs startup re-validation to detect Windows Update reverts (D-05).
/// </summary>
public interface ITweakStateService
{
    /// <summary>
    /// Returns the persisted status for the given tweak ID.
    /// </summary>
    Task<TweakStatus> GetStatusAsync(string tweakId);

    /// <summary>
    /// Updates the persisted status for the given tweak ID with timestamp.
    /// </summary>
    Task UpdateAsync(string tweakId, TweakStatus status);

    /// <summary>
    /// Returns all persisted tweak statuses.
    /// </summary>
    Task<IReadOnlyDictionary<string, TweakStatus>> GetAllStatusAsync();

    /// <summary>
    /// Startup re-validation: reads each persisted "Applied" tweak's registry value
    /// and compares against the expected value. If mismatched (e.g. Windows Update
    /// reverted the tweak), sets status to "NotApplied" (D-05, ENG-06).
    /// </summary>
    Task<IReadOnlyList<string>> RevalidateAsync(IEnumerable<TweakDefinition> tweaks);
}
