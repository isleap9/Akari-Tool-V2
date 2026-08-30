using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Akari.App.ViewModels;

/// <summary>
/// Main application ViewModel orchestrates the modular checklist UI.
/// Loads tweak categories from the catalog, manages selection state,
/// and coordinates batch apply/revert via ITweakEngine.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ITweakCatalog _catalog;
    private readonly ITweakEngine _engine;
    private readonly ITweakStateService _stateService;
    private readonly ILogService _logService;

    public ObservableCollection<TweakCategoryViewModel> Categories { get; }

    [ObservableProperty]
    private double _batchProgress;

    [ObservableProperty]
    private string _batchStatusText = "Ready";

    [ObservableProperty]
    private bool _isBatchApplying;

    /// <summary>
    /// Whether all tweaks are currently selected (for Select All checkbox).
    /// </summary>
    public bool HasAllSelected
    {
        get => Categories.Any() && Categories.All(c => c.Tweaks.All(t => t.IsSelected));
        set
        {
            foreach (var category in Categories)
            {
                foreach (var tweak in category.Tweaks)
                {
                    tweak.IsSelected = value;
                }
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTweaks));
        }
    }

    public MainViewModel(
        ITweakCatalog catalog,
        ITweakEngine engine,
        ITweakStateService stateService,
        ILogService logService)
    {
        _catalog = catalog;
        _engine = engine;
        _stateService = stateService;
        _logService = logService;
        Categories = new ObservableCollection<TweakCategoryViewModel>();
    }

    /// <summary>
    /// Loads tweaks from the catalog, groups by category, creates ViewModels,
    /// and loads persisted state for each tweak.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _logService.LogAsync(LogLevel.Info, "MainViewModel: Loading tweak catalog...");
        var tweaks = await _catalog.GetAllAsync();
        var state = await _stateService.GetAllStatusAsync();

        var grouped = tweaks
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .GroupBy(t => t.Category)
            .ToList();

        foreach (var group in grouped)
        {
            var viewModels = group.Select(def => new TweakViewModel(def, _engine, _logService))
                .ToList();

            foreach (var vm in viewModels)
            {
                var status = state.GetValueOrDefault(vm.Id, TweakStatus.NotApplied);
                vm.IsApplied = status == TweakStatus.Applied;
                vm.StatusText = vm.IsApplied ? "Applied" : "Not Applied";
            }

            var categoryVm = new TweakCategoryViewModel(group.Key, group.Key, viewModels);
            Categories.Add(categoryVm);
        }

        await _logService.LogAsync(LogLevel.Info,
            $"MainViewModel: Loaded {tweaks.Count} tweaks across {Categories.Count} categories.");
    }

    /// <summary>
    /// Gets all selected tweak ViewModels across all categories.
    /// </summary>
    private IEnumerable<TweakViewModel> GetSelectedTweaks()
    {
        return Categories.SelectMany(c => c.Tweaks).Where(t => t.IsSelected);
    }

    /// <summary>
    /// Whether any tweak is currently selected for batch apply.
    /// </summary>
    public bool HasSelectedTweaks => GetSelectedTweaks().Any();

    /// <summary>
    /// Applies all selected tweaks in a batch with progress tracking.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApplySelected))]
    public async Task ApplyAllAsync()
    {
        var selected = GetSelectedTweaks().ToList();
        if (selected.Count == 0) return;

        IsBatchApplying = true;
        BatchStatusText = $"Applying {selected.Count} tweaks...";
        BatchProgress = 0;

        try
        {
            var ids = selected.Select(t => t.Id).ToList();
            var results = await _engine.ApplyBatchAsync(ids);

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var vm = selected.FirstOrDefault(t => t.Id == result.TweakId);
                if (vm != null)
                {
                    vm.IsApplied = result.Success && result.Status == TweakStatus.Applied;
                    vm.StatusText = result.Success
                        ? "Applied"
                        : $"Failed: {result.ErrorMessage}";
                }
                BatchProgress = (i + 1.0) / results.Count * 100.0;
                BatchStatusText = $"Applied {i + 1}/{results.Count}";
            }

            if (results.All(r => r.Success))
            {
                BatchStatusText = "All tweaks applied successfully";
            }
            else
            {
                var failed = results.Count(r => !r.Success);
                BatchStatusText = $"{failed} tweak(s) failed - check log file";
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Batch apply failed", ex);
            BatchStatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBatchApplying = false;
        }
    }

    private bool CanApplySelected() => !IsBatchApplying;

    /// <summary>
    /// Reverts all selected tweaks.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApplySelected))]
    public async Task RevertAllAsync()
    {
        var selected = GetSelectedTweaks().ToList();
        if (selected.Count == 0) return;

        IsBatchApplying = true;
        BatchStatusText = $"Reverting {selected.Count} tweaks...";
        BatchProgress = 0;

        try
        {
            var ids = selected.Select(t => t.Id).ToList();
            var results = await _engine.RevertBatchAsync(ids);

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var vm = selected.FirstOrDefault(t => t.Id == result.TweakId);
                if (vm != null)
                {
                    vm.IsApplied = result.Status == TweakStatus.NotApplied;
                    vm.StatusText = vm.IsApplied ? "Not Applied" : $"Failed: {result.ErrorMessage}";
                }
                BatchProgress = (i + 1.0) / results.Count * 100.0;
                BatchStatusText = $"Reverted {i + 1}/{results.Count}";
            }

            if (results.All(r => r.Success))
            {
                BatchStatusText = "All tweaks reverted successfully";
            }
            else
            {
                var failed = results.Count(r => !r.Success);
                BatchStatusText = $"{failed} tweak(s) failed - check log file";
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Batch revert failed", ex);
            BatchStatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBatchApplying = false;
        }
    }
}
