using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using AppTemplate.Framework.Messaging;

namespace AppTemplate.Framework.Services;

/// <summary>
/// Observable state object that drives a global <see cref="InfoBar"/> in the app shell.
/// Also publishes <see cref="ShowInfoBarMessage"/> so view models can trigger it without
/// a direct reference to the service.
/// </summary>
public interface IInfoBarService
{
    bool IsOpen { get; set; }
    string Title { get; set; }
    string Message { get; set; }
    InfoBarSeverity Severity { get; set; }
    bool IsClosable { get; set; }

    void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational);
    void ShowInfo(string title, string message);
    void ShowSuccess(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
    void Hide();
}

public partial class InfoBarService : ObservableObject, IInfoBarService
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private bool _isClosable = true;

    public void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Title = title;
        Message = message;
        Severity = severity;
        IsOpen = true;
    }

    public void ShowInfo(string title, string message) => Show(title, message, InfoBarSeverity.Informational);
    public void ShowSuccess(string title, string message) => Show(title, message, InfoBarSeverity.Success);
    public void ShowWarning(string title, string message) => Show(title, message, InfoBarSeverity.Warning);
    public void ShowError(string title, string message) => Show(title, message, InfoBarSeverity.Error);
    public void Hide() => IsOpen = false;
}