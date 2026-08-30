using Akari.Engine.Core;
using Akari.Engine.Core.Models;
using Akari.Engine.Logging;
using Akari.Engine.Memory;
using Akari.Engine.Power;
using Akari.Engine.Processes;
using Akari.Engine.Registry;
using Akari.Engine.Services;
using Akari.Engine.Storage;
using Akari.Engine.Tweaks;
using AppTemplate.Framework;
using AppTemplate.Framework.Logging;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Akari.App.ViewModels;
using Akari.App.Services;
using Akari.App.Views;

namespace Akari.App
{
    /// <summary>
    /// WinUI 3 application - bootstraps the DI host with the MVVM framework
    /// and the Akari engine, then launches the main window.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        /// <summary>Global service provider, usable from XAML bindings and non-DI code.</summary>
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>The primary application window.</summary>
        public static MainWindow? MainWindow { get; private set; }

        public static string AppName => "Akari Toolbox";
        public static string AppVersion =>
            typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        /// <summary>Folder used for persisted JSON state and logs.</summary>
        public static string AppDataFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Akari", "App");

        public static string SettingsFilePath => Path.Combine(AppDataFolder, "tweak-state.json");
        public static string LogFolder => Path.Combine(AppDataFolder, "logs");

        /// <summary>Checks if the current process is running with admin elevation (UI-03).</summary>
        private static bool IsElevated()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return identity.Groups?.Any(g => g.Value == "S-1-5-32-544") ?? false;
        }

        public App()
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // UI-03: Require admin elevation. If not elevated, show a dialog
            // and relaunch with elevation via the runas verb (UAC prompt).
            // Note: with requireAdministrator in app.manifest, Windows always
            // prompts for elevation before launch — this block is a safety net
            // for cases where the manifest is absent (e.g. debugging).
            if (!IsElevated())
            {
                // Create a temporary window to host the ContentDialog
                // (Window.Current.Content is null during OnLaunched before the main window exists).
                var hidden = new Window();
                hidden.Content = new Grid();
                hidden.Activate();
                var dialog = new ContentDialog
                {
                    Title = "Administrator Required",
                    Content = "Akari Tool V2 requires administrator privileges to apply system tweaks. The app will now restart with elevated permissions.",
                    CloseButtonText = "OK",
                    XamlRoot = hidden.Content.XamlRoot
                };
                dialog.ShowAsync().AsTask().Wait(TimeSpan.FromSeconds(30));

                var startInfo = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas" // triggers UAC prompt
                };
                Process.Start(startInfo);
                Shutdown();
                return;
            }

            // Single-instance: only the first process becomes the primary instance.
            var mainInstance = AppInstance.FindOrRegisterForKey("AkariToolbox");
            if (!mainInstance.IsCurrent)
            {
                var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
                mainInstance.RedirectActivationToAsync(activation).GetAwaiter().GetResult();
                Environment.Exit(0);
                return;
            }

            mainInstance.Activated += (_, _) => MainWindow?.Activate();

            _host = BuildHost();
            Services = _host.Services;

            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // Create and show the main window via DI.
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.Closed += (_, _) => Shutdown();
            MainWindow.Activate();

            // Initialize theme/culture asynchronously.
            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                var themeService = Services.GetRequiredService<IThemeService>();
                await themeService.InitializeAsync();
                MainWindow?.ApplyTheme(themeService.CurrentTheme);
            });
        }

        private static IHost BuildHost()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();
            Directory.CreateDirectory(LogFolder);
            builder.Logging.AddProvider(new FileLoggerProvider(LogFolder));

            // --- MVVM Framework services ---
            builder.Services.AddMvvmFramework();
            builder.Services.AddSingleton<LocalizedStrings>();
            builder.Services.AddSingleton<ISettingsStorage>(new FileSettingsStorage("Akari"));

            // Navigation (used by the MainWindow's NavigationView).
            builder.Services.AddSingleton<INavigationService>(sp =>
                new FrameNavigationService(pageType => (Page)ActivatorUtilities.CreateInstance(sp, pageType)));
            builder.Services.AddSingleton(sp => new Func<XamlRoot?>(() => MainWindow?.Content?.XamlRoot));
            builder.Services.AddSingleton(sp => new Func<IntPtr>(() =>
                MainWindow is null ? IntPtr.Zero : WinRT.Interop.WindowNative.GetWindowHandle(MainWindow)));

            // --- Akari Engine services ---
            builder.Services.AddSingleton<ILogService>(sp => new FileLogService(LogFolder));
            builder.Services.AddSingleton<IRegistryProvider, Win32RegistryProvider>();
            builder.Services.AddSingleton<ITweakStateService>(sp =>
                new JsonFileStateService(sp.GetRequiredService<IRegistryProvider>(), SettingsFilePath));

            // Manager classes
            builder.Services.AddSingleton<IServiceControllerFactory, ServiceControllerFactory>();
            builder.Services.AddSingleton<IProcessManager, ProcessManager>();
            builder.Services.AddSingleton<IPowerManager>(sp => new PowerManager(sp.GetRequiredService<IRegistryProvider>()));
            builder.Services.AddSingleton<IMemoryManager, MemoryManager>();

            // Executors
            builder.Services.AddSingleton<ITweakExecutor, RegistryTweakExecutor>();
            builder.Services.AddSingleton<ITweakExecutor, ServiceOperationExecutor>();
            builder.Services.AddSingleton<ITweakExecutor, ProcessOperationExecutor>();
            builder.Services.AddSingleton<ITweakExecutor, PowerOperationExecutor>();
            builder.Services.AddSingleton<ITweakExecutor, MemoryOperationExecutor>();

            // Catalog + Engine
            builder.Services.AddSingleton<ITweakCatalog>(sp => LoadTweakCatalog());
            builder.Services.AddSingleton<ITweakEngine>(sp =>
            {
                var catalog = sp.GetRequiredService<ITweakCatalog>();
                var executors = sp.GetRequiredService<IEnumerable<ITweakExecutor>>();
                var state = sp.GetRequiredService<ITweakStateService>();
                var log = sp.GetRequiredService<ILogService>();
                return new TweakEngine(catalog, executors, state, log);
            });

            // ViewModels + Views + Window
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<TweaksPage>();
            builder.Services.AddSingleton<HomePage>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<SettingsPage>();
            builder.Services.AddSingleton<MainWindow>();

            return builder.Build();
        }

        private static ITweakCatalog LoadTweakCatalog()
        {
            var assembly = typeof(App).Assembly;
            using var stream = assembly.GetManifestResourceStream("Akari.App.tweaks.json");
            if (stream == null)
            {
                throw new InvalidOperationException(
                    "tweaks.json not found as embedded resource in " + assembly.FullName);
            }
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
            var tweaks = JsonSerializer.Deserialize<List<TweakDefinition>>(json, options) ?? new();
            return new JsonTweakCatalog(tweaks);
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unhandled application exception");
            if (MainWindow?.Content?.XamlRoot is null) return;
            e.Handled = true;
            DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                try
                {
                    var dialogs = Services!.GetRequiredService<IDialogService>();
                    await dialogs.ShowErrorAsync(
                        "Something went wrong",
                        $"The app ran into an unexpected error and needs to close.\n\nDetails were logged to:\n{LogFolder}");
                }
                catch { /* never re-enter the crash handler */ }
                finally { Shutdown(); }
            });
        }

        private void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Services?.GetService<ILogger<App>>()?.LogError(e.ExceptionObject as Exception, "AppDomain unhandled exception");
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        }

        /// <summary>Disposes the DI host (flushing loggers) and terminates the app.</summary>
        private void Shutdown()
        {
            try { _host?.Dispose(); }
            catch { /* never prevent exit */ }
            _host = null;
            Exit();
        }
    }
}