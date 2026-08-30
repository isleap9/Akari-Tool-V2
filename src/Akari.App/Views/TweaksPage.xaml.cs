using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Akari.App.ViewModels;

namespace Akari.App.Views;

/// <summary>
/// The main gaming tweaks checklist page. Displays all tweak categories
/// dynamically from the catalog with per-tweak Apply/Revert and batch apply.
/// </summary>
public sealed partial class TweaksPage : Page
{
    /// <summary>Resolved from DI via App.Services.</summary>
    public MainViewModel ViewModel { get; }

    public TweaksPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }
}
