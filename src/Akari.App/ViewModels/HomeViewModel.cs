using CommunityToolkit.Mvvm.Input;
using Akari.App.Views;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;

namespace Akari.App.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    public HomeViewModel(INavigationService navigation, IDialogService dialogs)
    {
        _navigation = navigation;
        _dialogs = dialogs;
        Title = "Home";
    }

    [RelayCommand]
    private void OpenSettings() => _navigation.NavigateTo<SettingsPage>();

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        await _dialogs.ShowInfoAsync(
            $"About {App.AppName}",
            $"{App.AppName}\nVersion {App.AppVersion}\n\nSettings are stored in:\n{App.SettingsFilePath}");
    }
}
