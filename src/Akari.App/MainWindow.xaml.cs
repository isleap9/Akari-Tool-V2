using Akari.App.ViewModels;
using Akari.App.Views;
using AppTemplate.Framework;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Akari.App;

/// <summary>
/// Main application window hosting a NavigationView with a Frame that
/// displays the toolkit pages (Tweaks, Settings). Theme is applied from
/// the IThemeService via ApplyTheme().
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigation;

    public MainWindow(MainViewModel viewModel, INavigationService navigation)
    {
        InitializeComponent();

        _navigation = navigation;
        _navigation.SetFrame(ContentFrame);

        // Set initial window size and center on screen.
        SetWindowPosition(1100, 700);
        _navigation.NavigateTo<TweaksPage>(null, preserveStack: false);
    }

    /// <summary>
    /// Applies the requested theme to the window's root element.
    /// Called from App.OnLaunched after the theme service initializes.
    /// </summary>
    public void ApplyTheme(AppTheme theme)
    {
        ElementTheme elementTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        RootNav.RequestedTheme = elementTheme;
    }

    /// <summary>
    /// Centers the window and sets its size via the AppWindow API.
    /// </summary>
    private void SetWindowPosition(int width, int height)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero) return;

        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow is null) return;

        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
        if (displayArea is null) return;

        var workArea = displayArea.WorkArea;
        var x = workArea.X + (workArea.Width - width) / 2;
        var y = workArea.Y + (workArea.Height - height) / 2;

        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
        appWindow.Move(new Windows.Graphics.PointInt32(Math.Max(0, x), Math.Max(0, y)));
    }

    /// <summary>
    /// Handles NavigationView selection changes — navigates the Frame
    /// to the page matching the selected menu item's Tag.
    /// </summary>
    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        var tag = item.Tag?.ToString();
        switch (tag)
        {
            case "Tweaks":
                _navigation.NavigateTo<TweaksPage>(null, preserveStack: false);
                break;
            case "Settings":
                _navigation.NavigateTo<SettingsPage>(null, preserveStack: false);
                break;
        }
    }
}
