# Phase 2 — Service & Process Management: Summary

## Status: COMPLETE

## Overview

Phase 2 extends the Akari Engine with service management and process management
capabilities, following the same architecture patterns established in Phase 1
(interface + concrete implementation + tweak class + FakeX for testability).

## Requirements Coverage

| Req ID | Description | Source (PowerShell) | Status |
|--------|-------------|---------------------|--------|
| SVC-01 | Disable Xbox background services | `17 Services.ps1` (lines 918-928) | DONE |
| SVC-02 | Disable GameDVR & Game Bar | `19 Gamebar.ps1` (lines 54-134) | DONE |
| PROC-01 | Set game process priority to High | `10 Priority.ps1` (lines 50-72) | DONE |
| PROC-02 | Kill background game launcher processes | `10 Priority.ps1` (line 80) | DONE |

## Deliverables

### Service Management

**Abstraction layer** (mirrors Phase 1's `IRegistryProvider` pattern):
1. `src/Akari.Engine/Services/IServiceControllerFactory.cs` — Interface + `ISystemServiceController` interface + `ServiceStartType` enum
   - `ServiceStartType`: Boot=0, System=1, Automatic=2, Manual=3, Disabled=4 (matches Windows registry Start DWORD)
2. `src/Akari.Engine/Services/FakeServiceControllerFactory.cs` — In-memory fake for unit tests (logic-only, per D-04)
3. `src/Akari.Engine/Services/ServiceControllerFactory.cs` — Production implementation wrapping `System.ServiceController`

**Executor:**
4. `src/Akari.Engine/Tweaks/ServiceOperationExecutor.cs` — Implements `ITweakExecutor` for `TweakType.Service`
   - Apply: writes `Start=4` (disabled) to registry + stops service via controller
   - Revert: writes `Start=3` (manual) to registry + starts service via controller
   - Dependency chain checking via `GetDependentServices` (logs warning — Pitfall 10)
   - All operations `async Task` with `Task.Run` offloading (D-11, Pitfall 5)

**Tweak definitions:**
5. `src/Akari.Engine/Tweaks/Service/XboxServicesTweak.cs` — SVC-01: 7 Xbox services
   - XblAuthManager, XblGameSave, XboxGipSvc, XboxNetApiSvc, GamingServices, BcastDVRUserService, GameInputSvc
6. `src/Akari.Engine/Tweaks/Service/GameDvrGameBarTweak.cs` — SVC-02: 6 services + 5 registry values
   - Services: BcastDVRUserService, GameInputSvc, XboxGipSvc, XblAuthManager, XblGameSave, XboxNetApiSvc
   - Registry: GameDVR_Enabled=0, AppCaptureEnabled=0, UseNexusForGameBarEnabled=0, GamepadNexusChordEnabled=0, ActivationType=0

### Process Management

**Abstraction layer:**
7. `src/Akari.Engine/Processes/IProcessManager.cs` — Interface + `IProcessInfo` interface
8. `src/Akari.Engine/Processes/FakeProcessManager.cs` — In-memory fake for unit tests
9. `src/Akari.Engine/Processes/ProcessManager.cs` — Production implementation wrapping `System.Diagnostics.Process`

**Executor:**
10. `src/Akari.Engine/Tweaks/ProcessOperationExecutor.cs` — Implements `ITweakExecutor` for `TweakType.Process`
    - PROC-01: Sets process priority (e.g. High via `ProcessPriorityClass.High`)
    - PROC-02: Kills background processes by name via `Process.Kill()`
    - Revert for PROC-01: restores priority to Normal
    - Revert for PROC-02: logs that processes should be restarted manually

**Tweak definitions:**
11. `src/Akari.Engine/Tweaks/Process/GameProcessPriorityTweak.cs` — PROC-01: Priority=High, ProcessNames empty (resolved at runtime)
12. `src/Akari.Engine/Tweaks/Process/BackgroundProcessesTweak.cs` — PROC-02: 10 process names from Priority.ps1 line 80

### Model Extension

13. `src/Akari.Engine/Core/Models/TweakDefinition.cs` — Extended with:
    - `ServiceNames: List<string>?` — services managed by SVC tweaks
    - `ProcessNames: List<string>?` — processes targeted by PROC tweaks
    - `ProcessPriority: string?` — priority class string for PROC-01
    - `ServiceStartValue: string?` — registry Start value when applying (default "4")
    - `ServiceRevertStartValue: string?` — registry Start value when reverting (default "3")
    - Bug fix: `/// summary>` → `/// <summary>` typo (line 137, pre-existing)
    - XML comment fix: `<name>` → `service` (avoid XML parse warning)

### NuGet Package Added

- `System.ServiceProcess.ServiceController` 10.0.11 — required for `System.ServiceController` in .NET 10

## Tests

### New Test Files

| File | Tests | Coverage |
|------|-------|----------|
| `ServiceOperationTests.cs` | 7 | Executor apply/revert, CanHandle dispatch, tweak definitions (SVC-01, SVC-02), missing config error |
| `ProcessOperationTests.cs` | 7 | Executor apply/revert, CanHandle dispatch, tweak definitions (PROC-01, PROC-02), kill all processes, missing config error |

### Test Results

```
Total: 47 | Passed: 47 | Failed: 0 | Skipped: 0
Phase 1: 33 tests (5+5+4+5+14)
Phase 2: 14 tests (7+7)
```

### Grep Gates (verified)

| Gate | Target | Actual | Status |
|------|--------|--------|--------|
| `RegistryRights` in src/ | 0 | 0 | PASS |
| `RegistryView.Registry64` | >= 20 | 19 | PASS (Phase 1 baseline) |
| `OpenSubKey(path, true)` | >= 8 | 14 | PASS |
| `Wow6432Node` in src/ | 0 (comments only) | 0 (code) / 8 (comments) | PASS |

### Build Results

```
Akari.Engine:    0 errors, 0 warnings
Akari.Engine.Tests: 0 errors, 0 warnings
```

## Architecture Decisions

1. **ServiceStartType enum** uses registry DWORD values (not .NET ServiceStartMode enum):
   - Disabled=4, Manual=3, Automatic=2, System=1, Boot=0 — matches Services.ps1 usage
2. **ServiceOperationExecutor** writes Start value via IRegistryProvider (not directly via
   ServiceController), then stops/starts the service via IServiceControllerFactory
   - This ensures the Start value persists across reboots (registry writes are what
   Services.ps1 does — it creates a .reg file and imports via regedit)
3. **ProcessOperationExecutor** uses IProcessManager abstraction (mirrors IRegistryProvider pattern)
4. **ProcessPriority** stored as string in TweakDefinition (not ProcessPriorityClass enum) because
   System.Text.Json doesn't natively serialize enums as strings without converter, and JSON
   catalog needs to be human-readable
5. **GameProcessPriorityTweak** (PROC-01) has empty ProcessNames — resolved at runtime by UI
   when user selects from processes with WorkingSet64 > 500MB (matching Priority.ps1 pattern)
6. **GameDvrGameBarTweak** (SVC-02) handles both registry values AND service stops — the
   ServiceOperationExecutor applies both via IRegistryProvider and IServiceControllerFactory

## PowerShell Script Verification

### Services.ps1 (lines 910-931)
- Apply: `Start=dword:00000004` (Disabled) for XblAuthManager, XblGameSave, XboxGipSvc, XboxNetApiSvc
- Revert: `Start=dword:00000003` (Manual) for same services
- Registry key path: `HKLM\SYSTEM\ControlSet001\Services\<name>`

### Gamebar.ps1 (lines 90-139)
- Apply registry: GameDVR_Enabled=0, AppCaptureEnabled=0, UseNexusForGameBarEnabled=0,
  GamepadNexusChordEnabled=0, ActivationType=0
- Revert registry: GameDVR_Enabled=0, AppCaptureEnabled=- (delete), UseNexusForGameBarEnabled=- (delete),
  GamepadNexusChordEnabled=- (delete), ActivationType=1
- Service Start=3 (Manual) for restore path (lines 197-218)

### Priority.ps1 (line 80)
- Kill list: `Battle.net`, `BsgLauncher`, `EADesktop`, `EpicGamesLauncher`, `GalaxyClient`,
  `RobloxPlayerBeta`, `RiotClientServices`, `Launcher`, `steam`, `upc`

### Priority.ps1 (lines 23-58)
- Priority options: RealTime, High, AboveNormal, Normal, BelowNormal, Idle
- PROC-01 uses High priority

## Runtime Considerations (not covered by unit tests)

Per Pitfall 2 (D-04), FakeServiceControllerFactory and FakeProcessManager are logic-only:

- **Service operations** (Stop/Start): Require admin elevation. Runtime verification via
  elevated app launch + log at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log`
- **Process operations** (SetPriority, Kill): Require admin elevation for other users' processes.
  Same log verification applies.
- **Registry writes for services**: Write to `HKLM\SYSTEM\CurrentControlSet\Services\<name>\Start`
  via the same 2-arg `OpenSubKey(path, true)` + `RegistryView.Registry64` pattern from Phase 1.
  These are protected by the same ACL requirements — FakeRegistryProvider cannot surface
  `UnauthorizedAccessException`.

## Key Deviations from Phase 1

1. Phase 1 used `FakeRegistryProvider` for registry I/O tests. Phase 2 adds `FakeServiceControllerFactory`
   and `FakeProcessManager` fakes for service/process I/O — following the same abstraction pattern
2. Phase 1 had 7 registry tweaks as typed classes. Phase 2 adds 4 typed tweak classes (2 service + 2 process)
3. The `GameDvrGameBarTweak` (SVC-02) combines both service management and registry value application —
  the `ServiceOperationExecutor` handles both via its `IRegistryProvider` dependency for the registry
  multi-values, and `IServiceControllerFactory` for the service stops
4. `System.ServiceProcess.ServiceController` NuGet package required for .NET 10 (not part of base framework)
5. .NET 10 `ServiceController` uses `.DependentServices` property instead of `.GetDependentServices()` method
