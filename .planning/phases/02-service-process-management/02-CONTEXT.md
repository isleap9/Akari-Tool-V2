# Phase 2: Service & Process Management — Context

**Gathered:** 2026-08-29
**Status:** Ready for planning
**Depends on:** Phase 1 (Engine & Registry Foundation) — complete

<domain>
## Phase Boundary

Add system operation executors for managing Windows services (Xbox background services, GameDVR/Game Bar) and process optimization (priority, background process management) on top of the verified working engine from Phase 1.

Phase 2 extends the existing ITweakExecutor Strategy pattern with two new executors:
- `ISystemOperationExecutor` — for services and processes (uses System.ServiceController, System.Diagnostics.Process)
- The existing engine dispatch (TweakEngine → ITweakExecutor by TweakType) must accommodate the new TweakType.Service and TweakType.Process values

The TweakDefinition model needs extension to carry service/process-specific data (service names, process names, priority classes). The existing JSON tweak catalog and state service from Phase 1 must continue to work unchanged for all tweak types.
</domain>

<decisions>
## Implementation Decisions

### Service Management
- **D-06:** Service operations use `System.ServiceController` for runtime service control (Stop/Start) and `IRegistryProvider` for Start=4 (disabled) registry writes — both via the StateService for persistence. The 2-arg `OpenSubKey(path, true)` pattern from Phase 1 must be used for all registry writes (Pitfall 1 still applies).
- **D-07:** Service executor must check dependency chains before stopping — enumerate `ServiceController.ServicesDependedOn` and warn. Prefer disable (Start=4) over stop for background services (Pitfall 10 from PITFALLS.md).
- **D-08:** Xbox services are managed via both registry Start=4 (persisted disable) AND ServiceController.Stop() (immediate stop) — the PowerShell reference uses both `sc stop` and `reg add ... Start=dword:00000004`. The tool must do both: write Start=4 to persist across reboots, then call Stop() to take effect immediately.

### Process Management
- **D-09:** Process priority uses `System.Diagnostics.Process` — `GetProcessByName`, `Process.StartInfo` with priority class via `ProcessPriorityClass` enum. Process identification by name or by interactive selection (matching the PowerShell `Get-Process | Where-Object` pattern).
- **D-10:** Background process management (PROC-02) — stop non-essential processes during gaming. The PowerShell source (`10 Priority.ps1`) shows a list of game launcher processes to stop: "Battle.net", "EADesktop", "EpicGamesLauncher", "GalaxyClient", "RobloxPlayerBeta", "RiotClientServices", "Launcher", "steam", "upcwpl". These are the canonical list to kill.
- **D-11:** Process operations must be async Task with Task.Run offloading (same pattern as Phase 1, Pitfall 5).

### Architecture
- **D-12:** Service and Process tweak types reuse the existing `TweakEngine` dispatch — no changes to the engine core. New executors are registered alongside `RegistryTweakExecutor` via DI. `CanHandle(TweakType.Service)` and `CanHandle(TweakType.Process)` respectively.
- **D-13:** Service tweaks use the same `TweakDefinition` model — service names go in a new `ServiceNames` list field, and the executor uses `IRegistryProvider` for Start=4 writes + `IServiceControllerFactory` abstraction for Stop/Start calls (so tests can use a fake).
</decisions>

<canonical_refs>
## Canonical References

### Core Project Files
- `.planning/ROADMAP.md` §Phase 2 — Phase 2 goal, plans, and success criteria
- `.planning/REQUIREMENTS.md` §SVC-01, SVC-02, PROC-01, PROC-02 — Locked requirements for this phase
- `.planning/STATE.md` §Accumulated Context — Phase 1 decisions that carry forward

### Research & Pitfalls
- `.planning/research/PITFALLS.md` — Service dependency chains (Pitfall 10), UI freezing (Pitfall 5)
- `.planning/research/ARCHITECTURE.md` — Clean layered architecture, engine dispatch, state service
- `.planning/research/STACK.md` — .NET 10, System.ServiceController, System.Diagnostics.Process
- `.planning/research/SUMMARY.md` §Phase 2 — ServiceOperationExecutor, ProcessOperationExecutor patterns

### Implementation References
- `.planning/phases/01-engine-registry-foundation/01-CONTEXT.md` — Phase 1 decisions (D-01 through D-05)
- `.planning/phases/01-engine-registry-foundation/01-SKELETON.md` — Architecture decisions, project structure
- `src/Akari.Engine/Tweaks/RegistryTweakExecutor.cs` — Pattern to follow (ITweakExecutor, async, logging, state tracking)

### Source References (AkariOS Tweaks PowerShell scripts)
- `AkariOS Tweaks/8 Advanced/17 Services.ps1` — Service Start=4 disable values for all Windows services
- `AkariOS Tweaks/6 Windows/19 Gamebar.ps1` — GameDVR/GameBar service disable pattern (BcastDVRUserService, GameInputSvc, XboxGipSvc, XblAuthManager, XblGameSave, XboxNetApiSvc)
- `AkariOS Tweaks/8 Advanced/10 Priority.ps1` — Process priority + background process kill list
</canonical_refs>

<specifics>
## Specific Ideas

### SVC-01: Xbox Background Services
The Xbox services to disable (from Gamebar.ps1 §"servicesoff" reg file, line 918-928):
- `XblAuthManager` — Xbox Live Auth Manager (Start=4 when disabled)
- `XblGameSave` — Xbox Live Game Save (Start=4 when disabled)
- `XboxGipSvc` — Xbox Game Input (Start=4 when disabled)
- `XboxNetApiSvc` — Xbox Networking (Start=4 when disabled)
- `GamingServices` / `GamingServicesNet` — Gaming Services
- `BcastDVRUserService` — GameDVR/Broadcast User Service
- `GameInputSvc` — GameInput Service

Default Start type for these services (from Services.ps1 "serviceson" section, lines 1850-1863): Start=3 (Manual)

The service executor will:
1. Write Start=4 to each service's registry key (`HKLM\SYSTEM\CurrentControlSet\Services\<name>`) using IRegistryProvider (2-arg OpenSubKey, RegistryView.Registry64)
2. Call ServiceController.Stop() for each running service
3. Track state via ITweakStateService

### SVC-02: GameDVR/Game Bar
From Gamebar.ps1 §"gamebaroff" (line 90-135) and §"gamebaron" (line 154-218):
- Disable via registry: `HKCU\System\GameConfigStore\GameDVR_Enabled=0`, `HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR\AppCaptureEnabled=0`
- Disable GameBar activation: `HKCU\Software\Microsoft\GameBar\UseNexusForGameBarEnabled=0`, `HKCU\Software\Microsoft\GameBar\GamepadNexusChordEnabled=0`
- Set `HKLM\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter\ActivationType=0`
- Services: `BcastDVRUserService` Start=4, `GameInputSvc` Start=4, `XboxGipSvc` Start=4, `XblAuthManager` Start=4, `XblGameSave` Start=4, `XboxNetApiSvc` Start=4

Revert restores all values to defaults and Start=3 for services.

### PROC-01: Process Priority for Active Games
From Priority.ps1 §"1. Already Running" (line 11): User selects a running process by ID (filtered by WorkingSet64 > 500MB) and sets priority:
- PriorityClass options: RealTime, High, AboveNormal, Normal, BelowNormal, Idle
- The C# implementation uses `Process.GetProcesses()`, filters by WorkingSet64 > 500MB, presents list to user, then sets `process.PriorityClass`

### PROC-02: Background Process Management
From Priority.ps1 §"2. Startup" (line 75-144): Stop game launcher processes before launching a game:
- Process kill list: Battle.net, BsgLauncher, EADesktop, EpicGamesLauncher, GalaxyClient, RobloxPlayerBeta, RiotClientServices, Launcher, steam, upcwpl
- After game exits, user can restart the launcher

The C# implementation provides a `BackgroundProcessManager` that can stop a configurable list of known background processes.
</specifics>

<deferred>
## Deferred Ideas

- Real-time process monitoring (watching for game launch automatically) — v2+ feature
- Automated process re-enable after game exit — requires persistent background service
- Process affinity management (CPU core pinning) — could be added as PROC-03 in future
- GPU-specific priority (NVIDIA/AMD control panel settings) — vendor-specific, needs research
</deferred>

---

*Phase: 2-Service & Process Management*
*Context gathered: 2026-08-29*
