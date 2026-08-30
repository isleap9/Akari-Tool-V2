using Akari.Engine.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Akari.App.ViewModels;

/// <summary>
/// ViewModel for a tweak category group (e.g. "Registry", "Power", "Memory").
/// Wraps ObservableCollection of TweakViewModels for the category.
/// </summary>
public class TweakCategoryViewModel : INotifyPropertyChanged
{
    public string DisplayName { get; }
    public string CategoryKey { get; }
    public ObservableCollection<TweakViewModel> Tweaks { get; }

    public TweakCategoryViewModel(string displayName, string categoryKey, IEnumerable<TweakViewModel> tweaks)
    {
        DisplayName = displayName;
        CategoryKey = categoryKey;
        Tweaks = new ObservableCollection<TweakViewModel>(tweaks);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
