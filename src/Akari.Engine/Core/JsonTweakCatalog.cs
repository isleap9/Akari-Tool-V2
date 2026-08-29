// JsonTweakCatalog — loads tweak definitions from a JSON catalog file.
//
// The catalog format is a JSON array of TweakDefinition objects.
// This enables adding tweaks without code changes (PLAN.md key design decision).
// The tweak classes (GameModeTweak, etc.) provide the canonical definitions;
// this catalog provides an alternative JSON-driven loading path for testing.

using System.Text.Json;
using Akari.Engine.Core.Models;

namespace Akari.Engine.Core;

/// <summary>
/// Loads tweak definitions from a JSON catalog file (alternative to the
/// static tweak class definitions, for testing and verification).
/// </summary>
public class JsonTweakCatalog : ITweakCatalog
{
    private readonly List<TweakDefinition> _tweaks;

    /// <summary>
    /// Initializes a new JsonTweakCatalog from a list of tweak definitions.
    /// </summary>
    public JsonTweakCatalog(IEnumerable<TweakDefinition> tweaks)
    {
        _tweaks = tweaks.ToList();
    }

    /// <summary>
    /// Loads a JsonTweakCatalog from a JSON file path.
    /// </summary>
    public static async Task<JsonTweakCatalog> FromFileAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        var tweaks = JsonSerializer.Deserialize<List<TweakDefinition>>(json, options) ?? new();
        return new JsonTweakCatalog(tweaks);
    }

    /// <inheritdoc/>
    public Task<TweakDefinition?> GetByIdAsync(string id) =>
        Task.FromResult(_tweaks.FirstOrDefault(t => t.Id == id));

    /// <inheritdoc/>
    public Task<IReadOnlyList<TweakDefinition>> GetAllAsync() =>
        Task.FromResult((IReadOnlyList<TweakDefinition>)_tweaks);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TweakDefinition>> GetByCategoryAsync(string category) =>
        Task.FromResult((IReadOnlyList<TweakDefinition>)_tweaks.Where(t => t.Category == category).ToList());
}
