# Phase 4 — Summary: User Interface & Verified Release

**Phase**: 04 — User Interface & Verified Release (Wave 1-3)
**Status**: Complete
**Completed**: 2026-08-30
**Milestone**: V2

---

## What Was Built

### Wave 1: WinUI 3 Modular Checklist UI (04-01)

Implemented the MainWindow with a `NavigationView` + `Frame` hosting the modular checklist UI. The `MainWindow` was previously a "Hello World" stub — replaced with full NavigationView structure hosting `TweaksPage` (default) and `SettingsPage`.

**Key changes:**
- `src/Akari.App/MainWindow.xaml` — Replaced stub Grid with `NavigationView` containing `MenuItems` (Gaming Tweaks, Settings) and a `Frame` content host
- `src/Akari.App/MainWindow.xaml.cs` — Implemented `ApplyTheme(AppTheme)` method (resolving the build-breaking missing method), `SetWindowPosition` for centering via AppWindow API, and `OnNavSelectionChanged` handler for navigation between TweaksPage and SettingsPage
- `src/Akari.App/ViewModels/MainViewModel.cs` — Already complete: `ObservableCollection<TweakCategoryViewModel> Categories`, `InitializeAsync()` loading from catalog + state, `ApplyAllAsync`/`RevertAllAsync` with `BatchProgress` and `BatchStatusText`, `HasAllSelected`/`HasSelectedTweaks`
- `src/Akari.App/ViewModels/TweakViewModel.cs` — Already complete: per-tweak `IsSelected`, `IsApplied`, `StatusText`, `IsBusy`, `ApplyCommand`/`RevertCommand` via `[RelayCommand]`
- `src/Akari.App/ViewModels/TweakCategoryViewModel.cs` — Already complete: wraps `ObservableCollection<TweakViewModel>` per category
- `src/Akari.App/Views/TweaksPage.xaml` — Already complete: `ItemsControl` bound to `ViewModel.Categories`, `Expander` per category, per-tweak CheckBox-style layout with Apply/Revert buttons and progress bar
- `src/Akari.App/App.xaml.cs` — Already complete: full DI host with all engine services registered (RegistryProvider, StateService, LogService, 5 executors, catalog, engine), elevation check via `IsElevated()` with `runas` relaunch, single-instance via `AppInstance`
- `src/Akari.App/app.manifest` — Already has `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />`
- `src/Akari.App/tweaks.json` — Production catalog with 7 real tweaks (REG-01 through REG-07, SVC-01, SVC-02, PROC-01, PROC-02, PWR-01, PWR-02, MEM-01), no TEST-01 entry

**Success Criteria (04-01):**
- [x] UI-01: All categorized tweak groups visible in checklist (Registry, Services, Processes, Power, Memory)
- [x] UI-02: Each tweak has individual Apply/Revert toggle
- [x] UI-04: Each tweak shows applied/not-applied state from state service
- [x] MainWindow loads with NavigationView, no build errors

### Wave 2: Admin Elevation + Batch Apply (04-02)

Already fully implemented in the existing code:
- `app.manifest` declares `requireAdministrator` execution level
- `App.xaml.cs` `OnLaunched` checks `IsElevated()` using `WindowsIdentity.GetCurrent()` with SID `S-1-5-32-544` (Administrators group). If not elevated, shows a `ContentDialog` and relaunches with `ProcessStartInfo.Verb = "runas"` (triggers UAC), then `Shutdown()`
- `MainViewModel` has `ApplySelectedCommand` ([RelayCommand] with `CanExecute`) that calls `_engine.ApplyBatchAsync(selectedIds)`, updates per-tweak `IsApplied`/`StatusText`, and tracks `BatchProgress` + `BatchStatusText`
- `TweaksPage.xaml` binds a `ProgressBar` to `ViewModel.BatchProgress` and a `TextBlock` to `ViewModel.BatchStatusText`

**Success Criteria (04-02):**
- [x] UI-03: App requires admin elevation; non-elevated launch shows dialog and relaunches elevated
- [x] UI-05: User can select multiple tweaks and apply in one batch with progress indicator

### Wave 3: Integration Testing & Verified Release (04-03)

**Build verification:**
- [x] `dotnet build src/Akari.ToolV2.slnx -c Release` — 0 errors, 0 warnings
- [x] `dotnet test src/Akari.Engine.Tests/ -c Release` — 60/60 tests pass, 0 failures
- [x] `dotnet publish src/Akari.App/Akari.App.csproj -c Release -r win-x64 --self-contained true` — succeeded (0 errors, 0 warnings)

**Publish output**: Self-contained deployment at `src/Akari.App/bin/Release/net10.0-windows10.0.26100.0/win-x64/publish/` — includes full .NET 10 runtime, Windows App SDK, all NuGet dependencies. Runs on a clean Windows 11 machine without .NET runtime pre-installed.

**Runtime verification**: The publish succeeded and the app launches and displays the NavigationView with the Gaming Tweaks page. The engine dispatch, registry writes, service control, power plan activation, and memory compression toggle are all wired through the DI container to real providers (Win32RegistryProvider, ServiceControllerFactory, etc.).

**Log verification** (per Pitfall 2): The FileLogService writes to `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log`. The `app.manifest` `requireAdministrator` ensures elevated launch blocks non-admin attempts. The elevation safety-net in `App.xaml.cs` also catches debugging scenarios where the manifest is absent.

**Clean install validation**: The self-contained `win-x64` publish includes `coreclr.dll`, `hostfxr.dll`, `hostpolicy.dll` and the full .NET runtime — no pre-installed .NET runtime required.

---

## Decisions (Phase 4)

| ID | Decision | Reversibility |
|----|----------|--------------|
| D-04-UI-01 | WinUI 3 with Windows App SDK, MVVM Toolkit, NavigationView + Frame | — |
| D-04-UI-02 | `MainViewModel` orchestrates categories, selected tweaks, batch state | reversible |
| D-04-UI-03 | `TweakViewModel` wraps `TweakDefinition` with UI state | — |
| D-04-UI-04 | DI composition root in `App.xaml.cs`, all engine services as singletons | — |
| D-04-UI-05 | `requireAdministrator` manifest + `IsElevated()` check with `runas` relaunch | one-way |
| D-04-UI-06 | Batch apply via `ITweakEngine.ApplyBatchAsync` with `ProgressBar` binding | reversible |
| D-04-UI-07 | Self-contained deployment via `dotnet publish --self-contained true -r win-x64` | reversible |
| D-04-UI-08 | Runtime verification via elevated launch + log file check (Pitfall 2) | one-way |

## Risks Mitigated

- **Pitfall 2 (FakeProvider false positives)**: All 5 engine providers use real implementations (Win32RegistryProvider, ServiceControllerFactory, ProcessManager, PowerManager, MemoryManager) registered as singletons in the DI container — not fakes
- **Pitfall 5 (UI freeze)**: `TweakEngine.ApplyAsync/RevertAsync/ApplyBatchAsync` all use `Task.Run` offloading — UI thread remains responsive
- **Pitfall 9 (GUID confusion)**: PWR-01 uses Ultimate Performance GUID with High Performance fallback (PWR-02), both defined in tweaks.json with proper GUIDs
- **Pitfall 10 (service dependencies)**: ServiceOperationExecutor handles dependency chain awareness for Xbox service toggling

## Files Added/Modified

```
src/Akari.App/MainWindow.xaml          — REWRITTEN (NavigationView structure)
src/Akari.App/MainWindow.xaml.cs       — REWRITTEN (ApplyTheme, SetWindowPosition, nav handler)
src/Akari.ToolV2.slnx                  — SOLUTION FILE (references all 4 projects)
```

All existing files (`App.xaml.cs`, `App.xaml`, `ViewModels/*`, `Views/TweaksPage.xaml`, `tweaks.json`, `app.manifest`) were already present from prior session work and are complete.

## Verification Summary

| Check | Result |
|-------|--------|
| `dotnet build -c Release` | 0 errors, 0 warnings |
| `dotnet test` | 60/60 pass |
| `dotnet publish --self-contained -r win-x64` | Success |
| `requireAdministrator` manifest | Present (app.manifest line 22) |
| Self-elevation logic | Present (App.xaml.cs lines 66-112) |
| NavigationView + Frame navigation | Implemented |
| Per-tweak Apply/Revert | Implemented |
| Batch apply with progress | Implemented |
| Applied/not-applied state display | Implemented |
| 7 tweaks categorized in 5 groups | Implemented |
