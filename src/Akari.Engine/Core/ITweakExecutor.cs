// ITweakExecutor — strategy interface for applying/reverting a tweak type.
//
// Each concrete executor handles one TweakType (e.g. Registry). The engine
// dispatches by calling the executor whose CanHandle() returns true for
// the tweak's Type field (Strategy pattern per PLAN.md task 1, success criteria #1).

using Akari.Engine.Core.Models;

namespace Akari.Engine.Core;

/// <summary>
/// Executor strategy interface: each implementation handles one <see cref="TweakType"/>.
/// The <see cref="TweakEngine"/> dispatches apply/revert calls to the executor whose
/// <see cref="CanHandle(TweakType)"/> returns true (Strategy pattern, D-01).
/// </summary>
public interface ITweakExecutor
{
    /// <summary>
    /// Returns true if this executor can handle the given tweak type.
    /// Used by TweakEngine for Strategy-pattern dispatch (ENG-02).
    /// </summary>
    bool CanHandle(TweakType type);

    /// <summary>
    /// Asynchronously applies the tweak described by <paramref name="definition"/>.
    /// Must be async Task per D-05/ENG-04 — no synchronous blocking.
    /// </summary>
    Task<TweakResult> ApplyAsync(TweakDefinition definition);

    /// <summary>
    /// Asynchronously reverts the tweak described by <paramref name="definition"/>.
    /// Must be async Task per D-05/ENG-04 — no synchronous blocking.
    /// </summary>
    Task<TweakResult> RevertAsync(TweakDefinition definition);
}
