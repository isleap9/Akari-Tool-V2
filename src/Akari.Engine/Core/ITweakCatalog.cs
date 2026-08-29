// ITweakCatalog — provides tweak definitions from the JSON-driven catalog.
// Supports adding tweaks without code changes (PLAN.md key design decision).

using Akari.Engine.Core.Models;

namespace Akari.Engine.Core;

/// <summary>
/// Provides access to tweak definitions. The implementation loads from a JSON
/// catalog file (tweaks.json) — this enables adding or modifying tweaks without
/// code changes, supporting the modular checklist UI (Phase 4).
/// </summary>
public interface ITweakCatalog
{
    /// <summary>
    /// Returns the tweak definition with the given ID, or null if not found.
    /// </summary>
    Task<TweakDefinition?> GetByIdAsync(string id);

    /// <summary>
    /// Returns all tweak definitions in the catalog.
    /// </summary>
    Task<IReadOnlyList<TweakDefinition>> GetAllAsync();

    /// <summary>
    /// Returns all tweak definitions in a specific category (e.g. "Registry").
    /// </summary>
    Task<IReadOnlyList<TweakDefinition>> GetByCategoryAsync(string category);
}
