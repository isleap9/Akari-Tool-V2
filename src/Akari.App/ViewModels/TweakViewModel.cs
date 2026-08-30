using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Akari.App.ViewModels;

/// <summary>
/// ViewModel for a single tweak in the UI checklist.
/// Wraps TweakDefinition with UI state: IsSelected, IsApplied, and Apply/Revert commands.
/// Implements INotifyPropertyChanged via CommunityToolkit MVVM.
/// </summary>
public partial class TweakViewModel : ObservableObject
{
    private readonly ITweakEngine _engine;
    private readonly ILogService _logService;

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool RequiresRestart { get; }
    public bool RequiresAdmin { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isApplied;

    [ObservableProperty]
    private string _statusText = "Not Applied";

    [ObservableProperty]
    private bool _isBusy;

    public TweakViewModel(TweakDefinition definition, ITweakEngine engine, ILogService logService)
    {
        Id = definition.Id;
        Name = definition.Name;
        Description = definition.Description;
        RequiresRestart = definition.RequiresRestart;
        RequiresAdmin = definition.RequiresAdmin;
        _engine = engine;
        _logService = logService;
    }

    /// <summary>
    /// Applies this tweak via the engine. Updates IsApplied and StatusText.
    /// </summary>
    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Applying...";
        try
        {
            var result = await _engine.ApplyAsync(Id);
            if (result.Success)
            {
                IsApplied = true;
                StatusText = "Applied";
            }
            else
            {
                StatusText = $"Failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Failed to apply tweak {Id}", ex);
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Reverts this tweak via the engine. Updates IsApplied and StatusText.
    /// </summary>
    [RelayCommand]
    private async Task RevertAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Reverting...";
        try
        {
            var result = await _engine.RevertAsync(Id);
            if (result.Success)
            {
                IsApplied = false;
                StatusText = "Not Applied";
            }
            else
            {
                StatusText = $"Failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync($"Failed to revert tweak {Id}", ex);
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
