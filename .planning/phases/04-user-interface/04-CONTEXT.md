# Phase 4: User Interface & Verified Release - Context

**Gathered:** 2026-08-29
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver the WinUI 3 MVVM frontend for Akari Tool V2. The engine (Phases 1-3) is complete and verified: 60/60 tests pass, 0 build errors. Phase 4 wires the existing engine into a modular checklist UI, adds admin elevation enforcement, implements batch apply with progress, and performs end-to-end elevated runtime verification with self-contained deployment.

The architecture is layered: Akari.Engine (complete, .NET 10 class library) → Akari.App (new WinUI 3 MVVM project). The UI consumes ITweakCatalog, ITweakEngine, ITweakStateService, and ILogService as-is — no engine changes needed.
</domain>

<decisions>
## Implementation Decisions

### UI Architecture
- **D-04-UI-01:** Use WinUI 3 with Windows App SDK, MVVM Toolkit for data binding and commands. MainWindow uses NavigationView for category navigation. Each category is a section in the checklist.
- **D-04-UI-02:** `MainViewModel` orchestrates the overall UI state: list of categories (from tweak catalog), selected category, selected tweaks, apply/revert progress. `TweakCategoryViewModel` wraps an `ObservableCollection<TweakViewModel>` per category.
- **D-04-UI-03:** `TweakViewModel` is the per-tweak UI model: Id, Name, Description, IsSelected, IsApplied (from state service), RequiresRestart, RequiresAdmin. Commands: `ToggleCommand`, `ApplyCommand`, `RevertCommand`.
- **D-04-UI-04:** Dependency injection via `Microsoft.Extensions.DependencyInjection`. The composition root lives in App.xaml.cs — register all engine services (IRegistryProvider→Win32RegistryProvider, ITweakCatalog→JsonTweakCatalog, ITweakEngine→TweakEngine, ITweakStateService→JsonFileStateService, ILogService→FileLogService, plus all executors) and inject MainViewModel into MainWindow.

### Admin Elevation (UI-03)
- **D-04-UI-05:** `requireAdministrator` in the WinUI 3 app manifest (`Package.appxmanifest` or `app.manifest`). The app self-elevates via `Microsoft.Windows.SDK.Contracts` or by checking `WindowsIdentity.IsElevated` at startup — if not elevated, show a dialog and relaunch with `runas`.
- **Reversibility:** one-way — can't be added without manifest changes + relaunch logic

### Batch Apply (UI-05)
- **D-04-UI-06:** Batch apply uses `ITweakEngine.ApplyBatchAsync(tweakIds)` — already implemented in the engine. UI shows a progress indicator ( ProgressBar + status text) bound to the batch operation. Each TweakResult updates the corresponding TweakViewModel's IsApplied state.
- **Reversibility:** reversible — only UI binding changes

### Self-Contained Deployment
- **D-04-UI-07:** `dotnet publish -c Release -r win-x64 --self-contained true` with `SelfContained=true` and `RuntimeIdentifiers=win-x64` in the Akari.Tool.csproj. The app runs on a clean Windows 11 machine without .NET runtime pre-installed.
- **Reversibility:** reversible — only project property changes

### Runtime Verification Strategy (Pitfall 2 critical)
- **D-04-UI-08:** Unit tests with FakeRegistryProvider/FakePowerManager/FakeProcessManager/FakeServiceControllerFactory cover dispatch logic only — they DO NOT enforce ACLs and CANNOT catch `UnauthorizedAccessException` or permission failures. After building the UI, must: (1) launch the app elevated, (2) apply each tweak type from the UI, (3) verify the log file at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` contains no `UnauthorizedAccessException` entries, (4) verify each tweak type actually took effect in the real system (regedit, services.msc, powercfg, etc.).
- **Reversibility:** one-way — can't be retroactively added to a non-logging design

### Existing Engine Dependencies
- **Tweak catalog:** `tweaks.json` at `src/Akari.Engine.Tests/tweaks.json` has 8 entries (7 gaming tweaks + 1 test). Phase 4 should ship a `tweaks.json` with the 7 real gaming tweaks (no TEST-01 entry) at the app deployment root or embedded resource.
- **TweakType enum:** Registry, Service, Process, Power, Memory — all implemented. UI-01 requires categories: Registry, Services, Processes, Power, Memory, Network, Visual, Input. Note: there's no Network or Input TweakType yet — these are v2 requirements. Phase 4 UI should display categories dynamically from the catalog, so missing categories (Network, Input) simply won't appear.
- **Executors:** RegistryTweakExecutor, ServiceOperationExecutor, ProcessOperationExecutor, PowerOperationExecutor, MemoryOperationExecutor — all complete and registered via Strategy dispatch.
- **State service:** JsonFileStateService at `%LOCALAPPDATA%\Akari\App\state.json` — tracks Applied/NotApplied per tweak. Startup re-validation already implemented.

</decisions>

<canonical_refs>
## Canonical References

### Core Project Files
- `.planning/ROADMAP.md` §Phase 4 — Phase 4 goal, plans, and success criteria
- `.planning/REQUIREMENTS.md` §UI-01 through UI-02, UI-03, UI-04, UI-05 — Locked UI requirements
- `.planning/STATE.md` §Accumulated Context — Decisions from Phases 1-3 that carry forward
- `.planning/phases/01-engine-registry-foundation/01-SKELETON.md` — Project layout reference

### Research & Pitfalls
- `.planning/research/PITFALLS.md` — Pitfall 2 (FakeProvider false positives), Pitfall 5 (UI freeze), Pitfall 9 (GUID confusion), Pitfall 10 (service dependencies)
- `.planning/research/ARCHITECTURE.md` §Recommended Architecture — Clean layered architecture
- `.planning/research/STACK.md` — .NET 10, WinUI 3, Windows App SDK, MVVM Toolkit

### Implementation References for Downstream Agents
- `.hermes/HERMES.md` — Project instruction file (runtime: claude, stack: .NET 10)
- `src/Akari.Engine/` — Complete engine source: Core, Registry, Tweaks, Storage, Logging
- `src/Akari.Engine.Tests/tweaks.json` — 8-entry tweak catalog (7 real + 1 test)

### Existing Engine Interfaces (consumed by UI)
- `ITweakCatalog` — `GetAllAsync()`, `GetByCategoryAsync(category)`, `GetByIdAsync(id)`
- `ITweakEngine` — `ApplyAsync(id)`, `RevertAsync(id)`, `ApplyBatchAsync(ids)`, `GetStatusAsync(id)`
- `ITweakStateService` — `GetStatusAsync(id)`, `UpdateAsync(id, status)`, `GetAllStatusAsync()`, `RevalidateAsync(tweaks)`
- `ILogService` — `LogAsync(level, message)`, `LogErrorAsync(message, ex)`
- `TweakDefinition` — Id, Name, Category, Type, Description, RequiresRestart, RequiresAdmin, SortOrder + type-specific fields
- `TweakResult` — TweakId, Success, Status, ErrorMessage, ActualValue, Timestamp

**No external specs — requirements fully captured in ROADMAP.md §Phase 4 and REQUIREMENTS.md §UI-01 through UI-02, UI-03, UI-04, UI-05.**
</canonical_refs>

<specifics>
## Specific Ideas

- **Dynamic category display:** The UI should not hardcode 8 categories. Instead, read distinct `Category` values from the tweak catalog and create a `TweakCategoryViewModel` per category. This means Registry, Services, Processes, Power, Memory will appear (5 categories from the 7 tweaks). Network and Input won't appear since no tweaks exist for them yet — that's fine for v1.
- **TweakViewModel wrapping:** `TweakViewModel` wraps a `TweakDefinition` and adds UI state: `IsSelected` (binds to CheckBox), `IsApplied` (binds to state service), `ToggleCommand`, `ApplyCommand`, `RevertCommand`. The `IsApplied` property is initialized from `ITweakStateService.GetStatusAsync(tweakId)` at load time and updated after each operation.
- **NavigationView structure:** MainWindow uses `NavigationView` with `Pane` = category list (ListView bound to Categories collection), `Content` = the checklist for the selected category (ItemsControl bound to SelectedCategory.Tweaks). This gives the "modular checklist" UX.
- **Batch apply flow:** "Apply Selected" button at the bottom of the window → calls `ITweakEngine.ApplyBatchAsync(selectedTweakIds)` → binds a ProgressBar to the number completed / total → after completion, refreshes all TweakViewModel.IsApplied from state service.
- **Per-tweak apply/revert:** Each TweakViewModel has Apply and Revert buttons. Tapping them calls `ITweakEngine.ApplyAsync(id)` or `RevertAsync(id)` — these are already Task.Run-offloaded in the engine, so UI won't freeze (Pitfall 5).
- **Admin check at startup:** `App.xaml.cs` checks `Windows.Security.Principal.WindowsIdentity.GetCurrent().Owner.IsElevated` — if not elevated, show a ContentDialog explaining admin is required, then relaunch with elevation via `Process StartInfo.Verb = "runas"`.
- **tweaks.json location:** Ship a cleaned tweaks.json (7 real tweaks, no TEST-01) either as an embedded resource in Akari.Engine or as a content file in Akari.Tool that gets copied to the output directory. `JsonTweakCatalog.FromFileAsync` already supports both approaches.
- **DI registration order:** Register in App.xaml.cs: RegistryProvider → StateService → LogService → executors (all 5) → catalog (from file) → engine → viewmodels → MainWindow. Use `IServiceProvider` with `ActivatorUtilities` for ViewModelFactory.
</specifics>

<deferred>
## Deferred Ideas

- **Network QoS tweaks (NETWORK-*)** — no executor or tweak definitions exist. Requires Phase 2/3 patterns extended to network API (QoS policies via netsh or registry). Deferred to v2.
- **Input/mouse optimization beyond REG-07** — mouse acceleration is already covered by REG-07. Additional input tweaks deferred to v2.
- **Real-time monitoring/overlay (MON-01 through MON-03)** — explicitly out of scope per REQUIREMENTS.md Out of Scope.
- **Automated restore point creation (ADV-01)** — explicitly out of scope per PROJECT.md constraints.
- **Appx package management (APPX-01)** — deferred to v2.
- **GPU-specific optimizations (ADV-02)** — deferred to v2.

### Reviewed Todos (not folded)
None.
</deferred>

---

*Phase: 04-User Interface & Verified Release*
*Context gathered: 2026-08-29*
