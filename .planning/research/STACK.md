# Technology Stack: Akari Tool V2

**Project:** Akari Tool V2 — Windows 11 gaming optimization toolbox
**Researched:** 2026-08-29
**Overall confidence:** HIGH

## Recommended Stack

| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| .NET | 10.0 | Runtime, target framework | LTS release (Nov 2025), best WinUI 3 and Windows API support, long-term support for a personal tool |
| Windows App SDK | 1.7.x | WinUI 3 hosting, Windows Runtime APIs | Stable channel (1.7.x is current stable as of 2025), required for WinUI 3 in desktop apps, ships via NuGet — no framework dependency on user machine |
| Microsoft.WindowsAppSDK | NuGet package | WinUI 3, MRT, Windows App SDK runtime | Self-contained deployment via Bundles; version 1.7.x stable |
| MVVM Toolkit | 8.4.2 | MVVM pattern implementation | Latest stable, compatible with .NET 10 and Windows App SDK 1.7, Microsoft-supported |
| Microsoft.Extensions.DependencyInjection | 8.x | DI container | Standard .NET DI, used by WinUI 3 template, clean ViewModel injection |
| Microsoft.Extensions.Configuration.Json | 8.x | Tweak catalog/config JSON loading | Standard .NET configuration, clean JSON deserialization |
| Microsoft.Extensions.Logging | 8.x | Structured logging | Standard .NET logging abstraction, FileLogService impl |
| CommunityToolkit.Mvvm | 8.4.2 | ObservableObject, RelayCommand attributes | Reduces boilerplate, integrates with DI |
| Microsoft.Win32.Registry | BCL | Registry read/write operations | Built into .NET, no external dep |
| System.ServiceProcess | BCL | Service start/stop management | Built into .NET, no external dep |
| System.Diagnostics.Process | BCL | Process management (priority, kill) | Built into .NET |
| System.Net.NetworkInformation | BCL | Network QoS inspection | Built into .NET |
| PowerCfg API (via cmd/PowerShell) | Windows built-in | Power plan management | powercfg.exe — most reliable for power plan activation |

## Core Framework

### .NET 10 + Windows App SDK 1.7 (Stable)

- .NET 10 released November 2025 as LTS, supports WinUI 3 via Windows App SDK
- Windows App SDK 1.7.x is the current stable channel (as of Aug 2026)
- Self-contained deployment: `SelfContained=true`, `RuntimeIdentifiers=win-x64` — no framework dependency on user machine
- WinUI 3 is the native UI framework for modern Windows desktop apps; no UWP dependency required

### MVVM Toolkit 8.4.2

- `CommunityToolkit.Mvvm` 8.4.2 — latest stable, confirmed compatible with .NET 10
- Provides `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` source generators — eliminates boilerplate
- Integrates with `Microsoft.Extensions.DependencyInjection` for ViewModel registration

### Architecture Pattern: Clean Layered (inspired by Microsoft docs)

- **Core layer**: Tweak definitions, engine interfaces, Operation types — no Win32/.NET Framework references
- **Infrastructure layer**: Concrete executors (RegistryOperationExecutor, ServiceOperationExecutor), providers (Win32RegistryProvider, ServiceControllerProvider), state management, logging
- **Presentation layer (Akari.App)**: WinUI 3 Views and ViewModels, DI registration in App.xaml.cs
- Reference: Microsoft's "Architecture patterns for WinUI 3 desktop apps" — dependency injection, configuration management, layered architecture

## Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| CommunityToolkit.Mvvm | 8.4.2 | MVVM, source generators | Always — core MVVM pattern |
| Microsoft.WindowsAppSDK | 1.7.x | WinUI 3 hosting | Always — UI framework |
| System.Management | BCL add-on | WMI queries (Appx packages) | Appx management feature |
| Newtonsoft.Json or System.Text.Json | BCL | JSON serialization | Tweak catalog loading |
| Microsoft.Extensions.Configuration | 8.x | Settings/config | Always — app configuration |

## Alternatives Considered

| Category | Recommended | Alternative | Why Not |
|----------|-------------|-------------|---------|
| UI Framework | WinUI 3 | WPF | WPF is legacy; WinUI 3 is modern, Microsoft-supported for new Windows 11 apps |
| UI Framework | WinUI 3 | UWP/Blazor Hybrid | UWP is deprecated; Blazor Hybrid overkill for a system tool |
| MVVM | CommunityToolkit.Mvvm 8.4.2 | Prism | Prism is heavier; MVVM Toolkit is lighter, MIT-licensed, Microsoft-maintained |
| DI | Microsoft.Extensions.DependencyInjection | Autofac | Built-in DI is sufficient for this project size |
| Registry | Microsoft.Win32.Registry | P/Invoke advapi32.dll | RegistryKey API is sufficient and safer; P/Invoke only needed for edge cases |
| Packaging | Self-contained exe | MSIX | Personal tool — self-contained exe is simplest for distribution |
| Deployment | Win-x64 only | Cross-platform | Tool is Windows 11-specific by nature |

## Key Technical Decisions

### Registry Access Pattern

- Use `Microsoft.Win32.RegistryKey` — the 2-arg `OpenSubKey(path, true)` (writable) overload, NOT the 3-arg overload with explicit `RegistryRights`
- The 3-arg `OpenSubKey(path, permissionCheck, rights)` does NOT cover all internal rights that `RegistryKey.SetValue()` needs (it needs to enumerate values and check permissions internally), causing `UnauthorizedAccessException` even when elevated
- The 2-arg writable overload handles this correctly
- For testing: use an `IRegistryProvider` abstraction with a `FakeRegistryProvider` (in-memory dict) — BUT FakeRegistryProvider does NOT enforce ACLs, so it cannot catch runtime-only `UnauthorizedAccessException` — must verify via actual elevated app launch + log check
- Target registry hives: HKLM (machine-wide) for system tweaks, HKCU (user) for per-user settings
- Use `RegistryView.Registry64` explicitly when accessing HKLM on 64-bit systems to avoid Wow6432Node redirection issues

### Service Management

- `System.ServiceController` for querying/starting/stopping services
- Requires admin elevation for stop/start operations
- Use `ServiceController.{Start,Stop,Pause,Continue}()` with timeout handling

### Process Management

- `System.Diagnostics.Process` for process inspection, priority setting, and optional termination
- `Process.GetProcessesByName()` for finding game/background processes
- `Process.PriorityClass` for setting process priority (RealTime, High, AboveNormal)

### Network QoS

- `NetQosPolicy` PowerShell cmdlets (`Get-NetQosPolicy`, `Set-NetQosPolicy`) for QoS policy management
- Direct WMI access to `ROOT\StandardCimv2` for programmatic QoS
- Alternatively: registry-based NetworkThrottlingIndex under `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile`

### Power Management

- `powercfg.exe` via `Process.Start()` for power plan activation/duplication
- Registry access for fine-grained power setting tweaks (GUID-based settings)

## Installation

```bash
# Core
dotnet new winui3 -o Akari.App
dotnet add package Microsoft.WindowsAppSDK
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Logging

# Self-contained deployment
dotnet publish -c Release -r win-x64 --self-contained true
```

## Sources

- Microsoft Learn — Windows App SDK: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/
- Microsoft Learn — Windows App SDK downloads: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads
- Microsoft Learn — Architecture patterns for WinUI 3: https://learn.microsoft.com/en-us/windows/apps/develop/architecture-patterns
- NuGet — CommunityToolkit.Mvvm 8.4.2: https://www.nuget.org/packages/CommunityToolkit.Mvvm
- Microsoft Docs — RegistryKey.OpenSubKey: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.opensubkey
- Microsoft Learn — MMCSS: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
- Microsoft Learn — Quality of Service (QoS) Policy: https://learn.microsoft.com/en-us/windows-server/networking/technologies/qos/qos-policy-top

**Confidence:** HIGH — all core technologies are well-documented, current, and confirmed compatible as of 2026.
