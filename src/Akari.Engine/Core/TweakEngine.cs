// TweakEngine — Strategy-pattern dispatch engine for tweak application/reversion.
//
// Dispatches ApplyAsync/RevertAsync to the ITweakExecutor whose CanHandle()
// returns true for the tweak's TweakType. All operations are async Task
// with Task.Run offloading (ENG-04). Supports batch application (ENG-05).

using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Storage;

namespace Akari.Engine.Core;

/// <summary>
/// Engine that dispatches tweak apply/revert operations via Strategy pattern
/// to the appropriate <see cref="ITweakExecutor"/> (ENG-02).
/// </summary>
public class TweakEngine : ITweakEngine
{
    private readonly ITweakCatalog _catalog;
    private readonly IEnumerable<ITweakExecutor> _executors;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public TweakEngine(
        ITweakCatalog catalog,
        IEnumerable<ITweakExecutor> executors,
        ITweakStateService stateService,
        ILogService logService)
    {
        _catalog = catalog;
        _executors = executors;
        _stateService = stateService;
        _logService = logService;
    }

    /// <summary>
    /// Dispatches application of the tweak with the given ID to the matching executor.
    /// Runs on a background thread via Task.Run to avoid UI freezing (ENG-05).
    /// </summary>
    public async Task<TweakResult> ApplyAsync(string tweakId)
    {
        return await Task.Run(async () =>
        {
            await _logService.LogAsync(LogLevel.Info, $"Applying tweak: {tweakId}");

            var definition = await _catalog.GetByIdAsync(tweakId);
            if (definition == null)
            {
                var notFoundError = new TweakResult
                {
                    TweakId = tweakId,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = $"Tweak not found in catalog: {tweakId}"
                };
                await _logService.LogErrorAsync("Tweak not found in catalog",
                    new KeyNotFoundException(tweakId));
                return notFoundError;
            }

            var executor = SelectExecutor(definition.Type);
            if (executor == null)
            {
                var noExecutorResult = new TweakResult
                {
                    TweakId = tweakId,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = $"No executor registered for tweak type: {definition.Type}"
                };
                await _logService.LogErrorAsync("No executor registered for type " + definition.Type,
                    new InvalidOperationException($"No executor for {definition.Type}"));
                return noExecutorResult;
            }

            var result = await executor.ApplyAsync(definition);

            if (result.Success)
            {
                await _stateService.UpdateAsync(tweakId, TweakStatus.Applied);
                await _logService.LogAsync(LogLevel.Info,
                    $"Tweak {tweakId} applied successfully");
            }
            else
            {
                await _logService.LogErrorAsync(
                    $"Failed to apply tweak {tweakId}: {result.ErrorMessage}",
                    result.ErrorMessage != null ? new Exception(result.ErrorMessage) : null);
            }

            return result;
        });
    }

    /// <summary>
    /// Dispatches reversion of the tweak with the given ID to the matching executor.
    /// </summary>
    public async Task<TweakResult> RevertAsync(string tweakId)
    {
        return await Task.Run(async () =>
        {
            await _logService.LogAsync(LogLevel.Info, $"Reverting tweak: {tweakId}");

            var definition = await _catalog.GetByIdAsync(tweakId);
            if (definition == null)
            {
                return new TweakResult
                {
                    TweakId = tweakId,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = $"Tweak not found in catalog: {tweakId}"
                };
            }

            var executor = SelectExecutor(definition.Type);
            if (executor == null)
            {
                return new TweakResult
                {
                    TweakId = tweakId,
                    Success = false,
                    Status = TweakStatus.NotApplied,
                    ErrorMessage = $"No executor registered for tweak type: {definition.Type}"
                };
            }

            var result = await executor.RevertAsync(definition);

            if (result.Success)
            {
                await _stateService.UpdateAsync(tweakId, TweakStatus.NotApplied);
                await _logService.LogAsync(LogLevel.Info,
                    $"Tweak {tweakId} reverted successfully");
            }
            else
            {
                await _logService.LogErrorAsync(
                    $"Failed to revert tweak {tweakId}: {result.ErrorMessage}",
                    result.ErrorMessage != null ? new Exception(result.ErrorMessage) : null);
            }

            return result;
        });
    }

    /// <summary>
    /// Applies multiple tweaks in a batch. All operations are async Task with
    /// Task.Run offloading (ENG-05). Returns results in the same order as input.
    /// </summary>
    public async Task<IReadOnlyList<TweakResult>> ApplyBatchAsync(IEnumerable<string> tweakIds)
    {
        var ids = tweakIds.ToList();
        return await Task.Run(async () =>
        {
            var results = new List<TweakResult>();
            foreach (var id in ids)
            {
                var result = await ApplyAsync(id);
                results.Add(result);
            }
            return (IReadOnlyList<TweakResult>)results;
        });
    }

    /// <summary>
    /// Returns the persisted status for the given tweak ID.
    /// </summary>
    public async Task<TweakStatus> GetStatusAsync(string tweakId)
    {
        return await _stateService.GetStatusAsync(tweakId);
    }

    /// <summary>
    /// Dispatches to the ITweakExecutor that CanHandle the given TweakType
    /// (Strategy pattern dispatch, success criteria #1).
    /// </summary>
    private ITweakExecutor? SelectExecutor(TweakType type)
    {
        return _executors.FirstOrDefault(e => e.CanHandle(type));
    }
}
