// ITweakEngine — the engine dispatch interface.
//
// Provides the Strategy-pattern entry points for applying, reverting,
// and batch-applying tweaks. The engine dispatches each operation to the
// ITweakExecutor that CanHandle the tweak's TweakType (D-01).

using Akari.Engine.Core.Models;

namespace Akari.Engine.Core;

/// <summary>
/// Engine that dispatches tweak apply/revert operations via Strategy pattern
/// to the appropriate <see cref="ITweakExecutor"/> (ENG-02).
/// </summary>
public interface ITweakEngine
{
    /// <summary>
    /// Asynchronously applies the tweak with the given ID.
    /// Returns a <see cref="TweakResult"/> indicating success or failure.
    /// </summary>
    Task<TweakResult> ApplyAsync(string tweakId);

    /// <summary>
    /// Asynchronously reverts the tweak with the given ID.
    /// </summary>
    Task<TweakResult> RevertAsync(string tweakId);

    /// <summary>
    /// Asynchronously applies multiple tweaks in a batch.
    /// Returns results for each tweak, in order.
    /// All operations are async Task with Task.Run offloading (ENG-05).
    /// </summary>
    Task<IReadOnlyList<TweakResult>> ApplyBatchAsync(IEnumerable<string> tweakIds);

    /// <summary>
    /// Asynchronously reverts multiple tweaks in a batch.
    /// Returns results for each tweak, in order.
    /// </summary>
    Task<IReadOnlyList<TweakResult>> RevertBatchAsync(IEnumerable<string> tweakIds);

    /// <summary>
    /// Returns the current status of the tweak with the given ID.
    /// </summary>
    Task<TweakStatus> GetStatusAsync(string tweakId);
}
