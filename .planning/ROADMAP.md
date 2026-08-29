# Roadmap: Akari Tool V2

## Overview

Build a WinUI 3 MVVM toolbox for Windows 11 gamers that consolidates system-level gaming optimizations into a single modular checklist. Users toggle categorized tweak groups (registry, services, processes, power, memory, visual, input) and apply them with admin elevation. The roadmap is organized as vertical slices: Phase 1 delivers a working registry engine with 7 gaming registry tweaks; Phase 2 adds service and process management; Phase 3 adds power and memory platform tweaks; Phase 4 delivers the full modular checklist UI with end-to-end elevated runtime verification and self-contained deployment.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

|- [x] **Phase 1: Engine & Registry Foundation** - Registry provider abstraction (2-arg OpenSubKey, RegistryView.Registry64), tweak engine dispatch (Strategy pattern), state service, logging, and 7 registry-based gaming tweaks (Game Mode, HAGS, NetworkThrottlingIndex, CPU priority, multimedia, visual effects, mouse acceleration)
- [ ] **Phase 2: Service & Process Management** - Service management (Xbox background services, GameDVR/Game Bar) and process priority/affinity optimization for active games
- [ ] **Phase 3: Power, Memory & Platform Tweaks** - Power plan management (Ultimate Performance, High Performance fallback) and Windows memory compression toggle
- [ ] **Phase 4: User Interface & Verified Release** - WinUI 3 modular checklist UI with categorized tweak groups, per-tweak toggle, admin elevation, batch apply with progress, and end-to-end elevated runtime verification + self-contained deployment

## Phase Details

### Phase 1: Engine & Registry Foundation

**Goal**: Deliver a working backend engine that can apply and revert 7 registry-based gaming tweaks end-to-end, with state tracking, logging, and startup re-validation to detect Windows Update reverts. Must bake in the 2-arg `OpenSubKey(path, true)` pitfall prevention and `RegistryView.Registry64` from day one — no room for FakeRegistryProvider false confidence.

**Depends on**: Nothing (first phase)

**Requirements**: ENG-01, ENG-02, ENG-03, ENG-04, ENG-05, ENG-06, REG-01, REG-02, REG-03, REG-04, REG-05, REG-06, REG-07

**Success Criteria** (what must be TRUE):
1. User launches the tool as admin and the log file at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` contains no `UnauthorizedAccessException` during registry tweak application.
2. User toggles Game Mode (REG-01) on/off and the registry value at `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList` is correctly set and verified at runtime.
3. User restarts the tool after a Windows Update and it correctly detects reverted registry tweaks, showing them as "not applied" via startup re-validation (ENG-06).
4. User applies all 7 registry tweaks in batch and all engine operations complete asynchronously without UI freezing (ENG-05).
5. User verifies via regedit that HAGS (REG-02) writes to `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode` in the 64-bit registry view (not Wow6432Node).

**Plans**: 3 plans

Plans:
- [x] 01-01: Registry provider abstraction — `IRegistryProvider` interface, `Win32RegistryProvider` with 2-arg `OpenSubKey(path, true)` and explicit `RegistryView.Registry64`; `FakeRegistryProvider` for unit tests (logic only, not ACL) ✓
- [x] 01-02: Tweak engine core — `ITweakEngine` Strategy dispatch, `ITweakExecutor` interface, `ITweakStateService` (JSON persistence + startup re-validation), `ILogService` (file logging to `%LOCALAPPDATA%\Akari\App\logs\`) ✓
- [x] 01-03: Implement all 7 registry tweaks (REG-01 through REG-07) — Game Mode, HAGS, NetworkThrottlingIndex, Win32PrioritySeparation, Multimedia Tasks\Games, Visual Effects, Mouse Acceleration ✓

### Phase 2: Service & Process Management

**Goal**: Add system operation executors for managing Windows services (Xbox background services, GameDVR/Game Bar) and process optimization (priority, background process management) on top of the engine from Phase 1.

**Depends on**: Phase 1

**Requirements**: SVC-01, SVC-02, PROC-01, PROC-02

**Success Criteria** (what must be TRUE):
1. User toggles Xbox background services off (SVC-01) and verifies via `services.msc` that Xbox Live Auth Manager, Xbox Live Game Save, Gaming Services, and GameDVR/Broadcast services are disabled/stopped.
2. User sets an active game process to High priority (PROC-01) and confirms the priority is applied (verifiable in Task Manager process details).
3. User can disable background processes during gaming (PROC-02) and the tool logs each process operation with target name, operation type, and outcome.
4. Service operations respect dependency chains — the tool warns before stopping a service with dependents (Pitfall #10) and uses disable (Start=4) over stop where appropriate.
5. All service and process operations are logged to the app log file with operation name, target, and success/failure status.

**Plans**: 2 plans

Plans:
- [ ] 02-01: Service operation executor — `ServiceOperationExecutor` using `System.ServiceController`; implements Xbox service toggling (SVC-01) and GameDVR/Game Bar disable (SVC-02) with dependency chain awareness
- [ ] 02-02: Process operation executor — `ProcessOperationExecutor` using `System.Diagnostics.Process`; implements process priority setting for active games (PROC-01) and background process management during gaming (PROC-02)

### Phase 3: Power, Memory & Platform Tweaks

**Goal**: Add power plan management (Ultimate Performance activation with High Performance fallback) and Windows memory compression toggle, completing the set of non-registry, non-service system tweak types.

**Depends on**: Phase 1 (engine), Phase 2 (service/process patterns established)

**Requirements**: PWR-01, PWR-02, MEM-01

**Success Criteria** (what must be TRUE):
1. User activates the Ultimate Performance power plan (PWR-01) and verifies via `powercfg /list` that it becomes the active scheme.
2. If the Ultimate Performance GUID is not found, the tool falls back to High Performance plan (PWR-02) and logs the fallback to the app log file (Pitfall #9 — GUID confusion).
3. User toggles Windows memory compression off (MEM-01) and verifies via `Get-MMAgent` that memory compression is disabled.
4. Each power/memory tweak's original value is captured in the state service (JSON) before application, enabling clean revert without system restore points.
5. User can revert any applied power or memory tweak and the original system state is restored without errors in the log.

**Plans**: 2 plans

Plans:
- [ ] 03-01: Power operation executor — `PowerOperationExecutor` using `powercfg.exe` via `Process.Start`; activates Ultimate Performance (PWR-01) with High Performance fallback (PWR-02) and GUID validation
- [ ] 03-02: Memory operation executor — `MemoryOperationExecutor` using PowerShell `Disable-MMAgent` for memory compression toggle (MEM-01) with state tracking

### Phase 4: User Interface & Verified Release

**Goal**: Deliver the full WinUI 3 modular checklist UI exposing all tweak categories with per-tweak toggle, applied/not-applied state display, admin elevation, batch apply with progress indicator — then perform end-to-end elevated runtime verification, log validation, and build the self-contained win-x64 deployment package.

**Depends on**: Phase 1, Phase 2, Phase 3

**Requirements**: UI-01, UI-02, UI-03, UI-04, UI-05

**Success Criteria** (what must be TRUE):
1. User sees all categorized tweak groups (Registry, Services, Processes, Power, Memory, Network, Visual, Input) in the modular checklist UI (UI-01).
2. User toggles individual tweaks on/off (UI-02), sees applied/not-applied state for each tweak (UI-04), and applies all selected tweaks in one batch with a progress indicator (UI-05).
3. Tool requests admin elevation on startup via `requireAdministrator` manifest (UI-03) — non-elevated launch is blocked from applying system changes.
4. End-to-end: user applies all tweak categories from the UI and each tweak is verified applied in the actual system with no errors in the log file (elevated runtime verification — FakeRegistryProvider tests cannot catch this).
5. Self-contained win-x64 deployment builds via `dotnet publish -c Release -r win-x64 --self-contained true` and runs on a clean Windows 11 machine without .NET runtime pre-installed.

**Plans**: 3 plans

Plans:
- [ ] 04-01: WinUI 3 modular checklist UI — `MainWindow` (NavigationView), `MainViewModel`, `TweakCategoryViewModel`; categorized groups from tweak catalog, per-tweak toggle (UI-01, UI-02)
- [ ] 04-02: Admin elevation + batch apply — `requireAdministrator` manifest (UI-03), applied/not-applied state display (UI-04), batch apply with progress indicator (UI-05)
- [ ] 04-03: Integration testing & verified release — elevated runtime verification with log check, end-to-end apply/revert flow, self-contained deployment build, clean-Install validation

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Engine & Registry Foundation | 3/3 | Complete | 01-01 ✓, 01-02 ✓, 01-03 ✓ |
| 2. Service & Process Management | 0/2 | Not started | - |
| 3. Power, Memory & Platform Tweaks | 0/2 | Not started | - |
| 4. User Interface & Verified Release | 0/3 | Not started | - |
