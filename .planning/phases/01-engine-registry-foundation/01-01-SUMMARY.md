---
phase: 01-engine-registry-foundation
plan: 01
type: execute
wave: 1
autonomous: true
status: complete
requirements: [ENG-01]
started: 2026-08-29T12:15:04.000Z
completed: 2026-08-29T12:35:00.000Z
---

# Plan 01-01 Summary: Registry Provider Abstraction

Completed: 2026-08-29

## Overview

Created the registry provider abstraction layer that all 7 Phase 1 registry tweaks depend on. This layer bakes in the critical pitfall preventions (2-arg `OpenSubKey(path, true)` instead of 3-arg `RegistryRights`; `RegistryView.Registry64` for HKLM) from day one.

## Tasks Executed

### Tracer: End-to-end registry write/read with 2-arg OpenSubKey pattern
- Created `src/Akari.Engine/Akari.Engine.csproj` (.NET 10 class library, `net10.0-windows10.0.19041.0`)
- Created `src/Akari.Engine/Registry/IRegistryProvider.cs` — interface with `GetValueAsync<T>`, `SetValueAsync`, `KeyExistsAsync`, `DeleteValueAsync`
- Created `src/Akari.Engine/Registry/Win32RegistryProvider.cs` — production impl using `RegistryKey.OpenBaseKey(hive, RegistryView.Registry64)` then `OpenSubKey(subKey, true)` (2-arg writable)
- Created `src/Akari.Engine/Registry/FakeRegistryProvider.cs` — in-memory `ConcurrentDictionary` for unit tests (logic-only, no ACL enforcement per D-04)
- Created `src/Akari.Engine.Tests/RegistryProviderTests.cs` — tracer tests
- Created `src/Akari.Engine.Tests/Akari.Engine.Tests.csproj`
- Created `.gitignore` for .NET
- Build: 0 errors, 0 warnings. Tests: 2 passed (round-trip, delete)

### Win32RegistryProvider full implementation
- All 4 `IRegistryProvider` methods fully implemented
- `ParseKeyPath` helper parses hive prefixes (HKLM:, HKCU:, HKEY_LOCAL_MACHINE\, etc.)
- `StripPrefix` helper handles colon, backslash, and bare prefix forms
- All operations wrapped in `Task.Run` for async offloading (D-01, ENG-04)
- Grep gates verified:
  - `RegistryRights` count = 0 (never use 3-arg overload)
  - `RegistryView.Registry64` count = 7 (used for all hive access)
  - `OpenSubKey(subKey, true)` count = 4 (2-arg writable pattern)

### FakeRegistryProvider unit tests (expanded to 5 tests)
- `FakeRegistryProvider_RoundTrip_SetsAndGetsValue` — SetValue then GetValue returns correct value
- `FakeRegistryProvider_DeleteValue_RemovesValue` — DeleteValue removes a previously set value
- `FakeRegistryProvider_KeyExistsAsync_ReturnsFalseForMissingKey` — KeyExistsAsync returns false for missing keys
- `FakeRegistryProvider_KeyExistsAsync_ReturnsTrueAfterSetValue` — KeyExistsAsync returns true after SetValue
- `FakeRegistryProvider_SetValueAsync_OverwritesExistingValue` — SetValueAsync replaces existing values
- All tests include D-04 comment about logic-only validation

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build` | 0 errors, 0 warnings |
| `dotnet test` (5 tests) | 5 passed, 0 failed |
| `grep -c "RegistryRights"` (must be 0) | 0 — PASS |
| `grep -c "RegistryView.Registry64"` (must be >0) | 7 — PASS |
| `grep -c "OpenSubKey(subKey, true)"` (must be >0) | 4 — PASS |
| Tracer feedback gate (re-run build+test) | PASS — 0 errors, 5/5 tests |
| Commit hash | 7a7a431 |

## Artifacts

- `src/Akari.Engine/Akari.Engine.csproj` — .NET 10 class library
- `src/Akari.Engine/Registry/IRegistryProvider.cs` — provider interface (4 async methods)
- `src/Akari.Engine/Registry/Win32RegistryProvider.cs` — production provider (2-arg OpenSubKey, RegistryView.Registry64)
- `src/Akari.Engine/Registry/FakeRegistryProvider.cs` — in-memory test provider (no ACL enforcement)
- `src/Akari.Engine.Tests/Akari.Engine.Tests.csproj` — test project (xUnit)
- `src/Akari.Engine.Tests/RegistryProviderTests.cs` — 5 unit tests
- `.gitignore` — .NET ignore patterns

## Requirements Traceability

| Requirement | Status | Notes |
|-------------|--------|-------|
| ENG-01 | Satisfied | IRegistryProvider interface defined; async Task pattern used throughout |

## Key Decisions (Carry-Forward)

- **D-01**: 2-arg `OpenSubKey(path, true)` — NOT 3-arg `RegistryRights` overload (prevents UnauthorizedAccessException even when elevated)
- **D-02**: Explicit `RegistryView.Registry64` for HKLM (prevents Wow6432Node redirection)
- **D-04**: FakeRegistryProvider is logic-only — runtime ACL verification requires elevated launch + log check (cannot be caught by unit tests)

## Next Plan

Plan 01-02: Tweak Engine Core — `ITweakEngine` (Strategy dispatch), `ITweakExecutor` interface, `ITweakStateService` (JSON persistence + startup re-validation), `ILogService` (file logging). Depends on 01-01 (completed).
