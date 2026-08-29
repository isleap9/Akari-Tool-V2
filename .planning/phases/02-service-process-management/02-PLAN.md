# Phase 02: Service & Process Management — Execution Plan

**Phase:** 02-service-process-management
**Goal:** Add system operation executors for managing Windows services (Xbox background services, GameDVR/Game Bar) and process optimization (priority, background process management) on top of the verified working engine from Phase 1.
**Granularity:** Coarse (2 plans)
**Model Profile:** Adaptive

---

## User Story (MVP)

**As a** Windows 11 gamer using Akari Tool V2
**I want to** disable Xbox background services and GameDVR/GameBar, and set my game process to High priority
**so that** my system dedicates all available resources to my active game without unnecessary background services consuming CPU, memory, and I/O.

---

## Plan Overview

| Plan | Name | Wave | Dependencies | Requirements | Est. Tokens | Type |
|------|------|------|-------------|-------------|-------------|------|
| 02-01 | Service Operation Executor | 1 | 01-03 (Phase 1 complete) | SVC-01, SVC-02 | 28K | Tracer + 1 auto |
| 02-02 | Process Operation Executor | 2 | 02-01 | PROC-01, PROC-02 | 28K | Tracer + 1 auto |

**Total:** 2 plans, 6 tasks, ~56K tokens estimated

---

## Wave Execution

- **Wave 1:** 02-01 — Service operation executor (ServiceExecutor, IServiceController abstraction, Xbox service tweaks SVC-01, GameDVR/GameBar SVC-02)
- **Wave 2:** 02-02 — Process operation executor (ProcessExecutor, IProcessManager abstraction, process priority PROC-01, background process management PROC-02)

---

## Dependency Graph

```
01-03 (Phase 1 complete)
    ↓
02-01 (Service Operation Executor)
    ↓
02-02 (Process Operation Executor)
```

All plans use tracer-first decomposition: each plan leads with one end-to-end tracer task, followed by expansion tasks.

---

## Key Decisions (from CONTEXT.md)

| Decision | ID | Impact |
|----------|----|--------|
| Service operations use ServiceController + Registry Start=4 | D-06 | Must use 2-arg OpenSubKey for registry writes (D-01 still applies) |
| Check dependency chains before stopping services | D-07 | Prevents cascading service failures (Pitfall 10) |
| Disable (Start=4) preferred over stop for background services | D-08 | Persisted disable survives reboot |
| Process priority via System.Diagnostics.Process | D-09 | Standard .NET ProcessPriorityClass enum |
| Background process kill list from PowerShell source | D-10 | Battle.net, EADesktop, EpicGamesLauncher, etc. |
| All operations async Task with Task.Run | D-11 | Pitfall 5 — no UI blocking |
| Service/Process executors registered via DI alongside RegistryTweakExecutor | D-12 | No changes to engine core; extend ITweakExecutor registration |
| Service tweaks use ServiceNames list in TweakDefinition | D-13 | Extends the model; executor uses IRegistryProvider + IServiceControllerFactory |

---

## Success Criteria (from ROADMAP.md)

1. User toggles Xbox background services off (SVC-01) and verifies via `services.msc` that Xbox Live Auth Manager, Xbox Live Game Save, Gaming Services, and GameDVR/Broadcast services are disabled/stopped.
2. User sets an active game process to High priority (PROC-01) and confirms the priority is applied (verifiable in Task Manager process details).
3. User can disable background processes during gaming (PROC-02) and the tool logs each process operation with target name, operation type, and outcome.
4. Service operations respect dependency chains — the tool warns before stopping a service with dependents (Pitfall #10) and uses disable (Start=4) over stop where appropriate.
5. All service and process operations are logged to the app log file with operation name, target, and success/failure status.

---

## Verification Strategy

**Unit tests (FakeServiceController, FakeProcessManager):** Validate logic for service disable/stop, process priority, background process kill
- `dotnet test src/Akari.Engine.Tests/ --filter "FullyQualifiedName~Service"` — service tests
- `dotnet test src/Akari.Engine.Tests/ --filter "FullyQualifiedName~Process"` — process tests

**Runtime verification (elevated launch + log check):** The ONLY way to catch service/process operation failures
- Requires admin elevation — `requireAdministrator` manifest (Phase 4) or manual Run As Admin
- Check `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` for:
  - No `UnauthorizedAccessException` entries (Pitfall 1 — 3-arg OpenSubKey)
  - No `Wow6432Node` in registry write logs (Pitfall 3 — RegistryView.Registry64)
  - Service stop/start logged with service name and outcome
  - Process priority changes logged with process name and new priority

---

## Files Produced

| File | Purpose |
|------|---------|
| `02-CONTEXT.md` | Phase context (decisions, refs, specifics) |
| `02-01-PLAN.md` | Service operation executor |
| `02-02-PLAN.md` | Process operation executor |
| `02-PLAN.md` | This orchestration summary |
| `02-01-SUMMARY.md` | Wave 1 completion summary |
| `02-02-SUMMARY.md` | Wave 2 completion summary |

---

## Requirement Coverage

| Requirement | Covered In Plan |
|-------------|----------------|
| SVC-01: Disable Xbox background services | 02-01 |
| SVC-02: Disable GameDVR/Game Bar | 02-01 |
| PROC-01: Process priority for active games | 02-02 |
| PROC-02: Disable background processes during gaming | 02-02 |

**Coverage: 4/4 requirements mapped ✓ | 0 unmapped**
