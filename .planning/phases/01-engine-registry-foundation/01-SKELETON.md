# Walking Skeleton: Akari Tool V2 — Phase 1

**Phase:** 01 — Engine & Registry Foundation
**Status:** Skeleton — Phase 1 MVP vertical slice
**Created:** 2026-08-29

> This skeleton is the thinnest possible end-to-end slice of Phase 1. It proves the architectural stack (DI, logging, registry provider, state service, tweak engine) with ONE real tweak (Game Mode — REG-01) wired through every layer. All other 6 tweaks are parameterized and will be added as expansion tasks after this skeleton is verified.

## Architectural Decisions

### Framework
- **.NET 10** — target framework `net10.0-windows10.0.19041.0`
- **WinUI 3 / Windows App SDK** — for Phase 4 UI; Phase 1 is a class library with no UI dependency
- **MVVM Toolkit** — deferred to Phase 4 (UI layer); Phase 1 engine is framework-agnostic

### Project Layout
```
Akari.ToolV2.sln
├── src/Akari.Engine/              # Core engine library (.NET 10 class library)
│   ├── Core/                      # ITweakEngine, ITweakExecutor, ITweakCatalog
│   ├── Registry/                  # IRegistryProvider, Win32RegistryProvider, FakeRegistryProvider
│   ├── Tweaks/                    # RegistryTweakExecutor, 7 tweak classes
│   ├── Storage/                   # ITweakStateService, JsonFileStateService
│   └── Logging/                   # ILogService, FileLogService
├── src/Akari.Engine.Tests/        # xUnit test project
└── src/Akari.Tool/               # WinUI 3 app (Phase 4)
```

### Dependency Injection
- `Microsoft.Extensions.DependencyInjection` (10.0.11) — service container
- `Microsoft.Extensions.Logging` (10.0.11) — logging abstraction
- `Microsoft.Extensions.Configuration.Json` (10.0.11) — state file loading
- Services registered at the composition root; engine is testable without DI

### Registry Access
- `Microsoft.Win32.Registry` — built into .NET 10, no NuGet needed
- `IRegistryProvider` interface with `Win32RegistryProvider` (production) and `FakeRegistryProvider` (tests)
- **2-arg `OpenSubKey(path, true)`** — NOT the 3-arg `RegistryRights` overload [per D-01]
- **`RegistryView.Registry64`** explicitly specified for HKLM access [per D-01]

### Async Pattern
- All engine operations are `async Task` with `Task.Run` offloading for blocking registry I/O
- `ConfigureAwait(false)` on all internal awaits to prevent context deadlocks

### Logging
- File-based logging to `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` [per D-03]
- `ILogService` interface with `LogAsync`, `LogErrorAsync` methods
- Log entries include timestamp, level, message, and exception detail

### State Persistence
- JSON file at `%LOCALAPPDATA%\Akari\App\state.json` [per D-02]
- `ITweakStateService` with `UpdateAsync`, `GetStatusAsync`, `RevalidateAsync`
- Startup re-validation checks all persisted "Applied" tweaks against actual registry values [per D-05]

### Test Strategy
- **xUnit** test framework
- `FakeRegistryProvider` for unit tests (logic verification only — NOT for ACL verification [per D-04])
- Unit tests cover: provider abstraction, engine dispatch, state persistence, tweak definitions
- Runtime integration tests require admin elevation — verified via elevated launch + log file check

## The One Slice

**TWEAK: Game Mode (REG-01)**

End-to-end flow:
1. Load tweak definition from JSON catalog → `TweakDefinition` for Game Mode
2. Dispatch to `RegistryTweakExecutor` via `TweakEngine.ApplyAsync("REG-01")`
3. `Win32RegistryProvider.SetValueAsync` opens HKLM via `OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)` → `OpenSubKey(path, true)` → `SetValue`
4. `FileLogService.LogAsync` writes to `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log`
5. `JsonFileStateService.UpdateAsync` writes tweak status to `state.json`
6. Verify via log file: no `UnauthorizedAccessException` entries
7. Verify via `FakeRegistryProvider`: tweak state correctly persisted

## What Gets Built

| Layer | Component | File |
|-------|-----------|------|
| Core | `ITweakEngine`, `ITweakExecutor`, `ITweakCatalog`, `TweakDefinition`, `TweakResult`, `TweakStatus` | `Core/*.cs` |
| Registry | `IRegistryProvider`, `Win32RegistryProvider`, `FakeRegistryProvider` | `Registry/*.cs` |
| Tweaks | `ITweak`, `RegistryTweak`, `GameModeTweak`, `RegistryTweakExecutor` | `Tweaks/*.cs` |
| Storage | `ITweakStateService`, `JsonFileStateService`, `TweakStateEntry` | `Storage/*.cs` |
| Logging | `ILogService`, `FileLogService` | `Logging/*.cs` |
| Tests | `RegistryProviderTests`, `TweakEngineTests`, `StateServiceTests`, `GameModeTweakTests` | `Akari.Engine.Tests/*Tests.cs` |

## What Gets Deferred

- 6 remaining registry tweaks (REG-02 through REG-07) — expansion tasks
- Service/process management (Phase 2)
- Power/memory tweaks (Phase 3)
- WinUI 3 UI (Phase 4)

## Acceptance

The skeleton is accepted when:
1. `dotnet build` succeeds with 0 errors
2. `dotnet test` passes for FakeRegistryProvider-based unit tests
3. Elevated runtime launch + log file check confirms: no `UnauthorizedAccessException` in log for Game Mode toggle
