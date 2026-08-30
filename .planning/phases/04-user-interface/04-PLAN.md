# Phase 4 — PLAN.md (Orchestration)

## Phase Goal
Deliver the WinUI 3 MVVM modular checklist UI exposing all tweak categories with per-tweak toggle, applied/not-applied state display, admin elevation, batch apply with progress indicator — then perform end-to-end elevated runtime verification, log validation, and build the self-contained win-x64 deployment package.

## Depends On
Phase 1 (engine), Phase 2 (service/process), Phase 3 (power/memory) — all complete.

## Requirements
UI-01, UI-02, UI-03, UI-04, UI-05

## Wave Plan

### Wave 1: WinUI 3 Modular Checklist UI (04-01-PLAN.md)
**04-01**: MainWindow with NavigationView, MainViewModel, TweakCategoryViewModel, TweakViewModel; dynamic category display from tweak catalog; per-tweak toggle and applied state (UI-01, UI-02, UI-04)

Files:
- `src/Akari.App/Akari.App.csproj` — WinUI 3 project (new)
- `src/Akari.ToolV2.sln` — solution file (new)
- `src/Akari.App/App.xaml` + `App.xaml.cs` — DI composition root
- `src/Akari.App/MainWindow.xaml` + `MainWindow.xaml.cs` — NavigationView with category pane + checklist
- `src/Akari.App/ViewModels/MainViewModel.cs` — orchestrates categories, selected tweaks, batch state
- `src/Akari.App/ViewModels/TweakCategoryViewModel.cs` — wraps a category's tweak collection
- `src/Akari.App/ViewModels/TweakViewModel.cs` — per-tweak UI model with IsSelected, IsApplied, commands
- `src/Akari.App/tweaks.json` — cleaned catalog (7 real tweaks, no TEST-01)

Success Criteria:
- UI-01: All categorized tweak groups (Registry, Services, Processes, Power, Memory) visible in checklist
- UI-02: Each tweak has individual Apply/Revert toggle
- UI-04: Each tweak shows applied/not-applied state from state service
- MainWindow loads, displays all 7 tweaks in categorized groups, no build errors
- 0 errors, 0 warnings (except pre-existing CS1574 in engine)

### Wave 2: Admin Elevation + Batch Apply (04-02-PLAN.md)
**04-02**: requireAdministrator manifest, self-elevation logic, batch apply with progress indicator (UI-03, UI-05)

Files:
- `src/Akari.App/Package.appxmanifest` — declare requireAdministrator
- `src/Akari.App/app.manifest` — fallback elevation declaration
- `src/Akari.App/App.xaml.cs` — admin check at startup + relaunch with elevation
- `src/Akari.App/ViewModels/MainViewModel.cs` — Add SelectedTweakIds property, ApplySelectedCommand with progress binding
- `src/Akari.App/MainWindow.xaml` — "Apply Selected" button + ProgressBar

Success Criteria:
- UI-03: App requires admin elevation — non-elevated launch is blocked from applying changes
- UI-05: User can select multiple tweaks and apply in one batch with progress indicator
- App relaunches with elevation when not elevated
- State updates correctly after batch apply

### Wave 3: Integration Testing & Verified Release (04-03-PLAN.md)
**04-03**: End-to-end elevated runtime verification, log validation, self-contained win-x64 deployment build

Files:
- `src/Akari.App.Tests/Akari.App.Tests.csproj` — new test project (if needed)
- `src/Akari.App/tweaks.json` — final production catalog
- `.planning/phases/04-user-interface/04-SUMMARY.md` — milestone close-out

Success Criteria:
- End-to-end: user applies all tweak categories from the UI and each tweak is verified applied with no errors in the log file
- Log file at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` contains no UnauthorizedAccessException
- Self-contained win-x64 deployment builds via `dotnet publish -c Release -r win-x64 --self-contained true`
- Clean install validation on clean Windows 11 machine

## Wave Dependencies
- 04-01 → 04-02 (UI must exist before elevation/batch can be wired)
- 04-02 → 04-03 (must have working app before runtime verification)

## Verification
- `dotnet build src/Akari.ToolV2.sln -c Release` — 0 errors, 0 warnings (except pre-existing CS1574)
- `dotnet test src/Akari.Engine.Tests/` — all 60 tests still pass (no regressions)
- `dotnet publish src/Akari.App/Akari.App.csproj -c Release -r win-x64 --self-contained true` — succeeds
- Elevated runtime launch + log check: no UnauthorizedAccessException in log
