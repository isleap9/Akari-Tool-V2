# Plan 01-03 Summary — 7 Registry Tweaks + Batch Apply

**Status:** COMPLETE
**Wave:** 3
**Date:** 2026-08-29
**Commit:** Pending (Wave 3 commit)

## Overview

Plan 01-03 implements the 7 registry-based gaming tweaks (REG-01 through REG-07) as
typed C# classes, plus a JSON-driven catalog loader and a `RegistryTweakExecutor` that
dispatches apply/revert operations through the `IRegistryProvider` abstraction. This
completes the Phase 1 engine backend.

## Deliverables

### 1. RegistryTweakExecutor (src/Akari.Engine/Tweaks/RegistryTweakExecutor.cs)
- Implements `ITweakExecutor`
- Handles `TweakType.Registry` via Strategy-pattern dispatch
- Supports both single-value and multi-value registry tweaks
- All operations are `async Task` with `Task.Run` offloading (D-09)
- Uses `IRegistryProvider` abstraction for all registry operations (D-06)
- Uses `ITweakStateService` for state tracking and `ILogService` for logging
- `CanHandle(TweakType.Registry)` returns true; all other types return false

### 2. RegistryTweakExecutor — Key Design Decisions
- `ParseValueData` uses `unchecked((int)uint.Parse(valueData))` for DWORD values
  to handle values exceeding `int.MaxValue` (e.g. REG-03 NetworkThrottlingIndex = 0xFFFFFFFF)
  matching the .NET `RegistryKey.GetValue` behavior of returning signed `int` for DWORD
- Multi-value tweaks (REG-07) route through `RegistryMultiValues` list pattern
  instead of single `Registry` property
- `ApplyRegistryValuesAsync` parses multi-value data using `ParseValueData` to ensure
  type-safe registry operations

### 3. Seven Typed Tweak Classes (src/Akari.Engine/Tweaks/Registry/)

| ID | Class | Registry Path | Value Name | Enabled | Disabled | Hive |
|----|-------|--------------|------------|---------|----------|------|
| REG-01 | GameModeTweak | HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList | GameMode | 1 | 0 | HKLM |
| REG-02 | HagsTweak | HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers | HwSchMode | 2 | 1 | HKLM |
| REG-03 | NetworkThrottlingTweak | HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile | NetworkThrottlingIndex | 0xFFFFFFFF | 3 | HKLM |
| REG-04 | Win32PrioritySeparationTweak | HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl | Win32PrioritySeparation | 26 | 38 | HKLM |
| REG-05 | MultimediaTweak | HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games | Priority | 6 | 1 | HKLM |
| REG-06 | VisualEffectsTweak | HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects | VisualFXSetting | 2 | 3 | HKLM |
| REG-07 | MouseAccelerationTweak | HKCU\Control Panel\Desktop | MouseSpeed/Threshold1/Threshold2 | 0 | 0/6/10* | HKCU |

*REG-07 revert values: MouseSpeed=0, MouseThreshold1=6, MouseThreshold2=10 (Windows defaults)

### 4. JSON Tweak Catalog (src/Akari.Engine/Core/JsonTweakCatalog.cs)
- Loads tweak definitions from JSON files
- Uses `JsonSerializerOptions` with `JsonStringEnumConverter(JsonKnownNamingPolicy.CamelCase)`
  (fixed from `JsonStringEnumConverter.Default` which doesn't exist in .NET 10)
- Static `FromFileAsync` factory method for easy loading
- Test fixture at `src/Akari.Engine.Tests/tweaks.json` with all 7 definitions + 1 test tweak

### 5. TweakDefinition Model Extensions (src/Akari.Engine/Core/Models/TweakDefinition.cs)
- Added `RegistryMultiValues` (List<RegistryMultiValue>?) — for multi-value tweaks like REG-07
- Added `RegistryRevertValueData` (string?) — single-value revert data
- Added `RegistryMultiValue` class with Key, ValueName, ValueData, ValueKind properties
- Both new properties use `[JsonIgnore(Condition = WhenWritingNull)]`

## Verification Results

### Build
```
dotnet build src/Akari.Engine/Akari.Engine.csproj     — 0 errors, 0 warnings
dotnet build src/Akari.Engine.Tests/Akari.Engine.Tests.csproj — 0 errors, 0 warnings
```

### Tests
```
dotnet test   — Passed!  Failed: 0, Passed: 33, Skipped: 0, Total: 33
```

Test breakdown by file:
- RegistryProviderTests.cs: 5 tests (Wave 1, expanded)
- TweakEngineTests.cs: 5 tests (Wave 2)
- StateServiceTests.cs: 4 tests (Wave 2)
- LogServiceTests.cs: 5 tests (Wave 2)
- RegistryTweakTests.cs: 14 tests (Wave 3)

  - GameModeTweakTests: 3 tests — tracer apply/revert through executor, CanHandle dispatch, definition verification
  - AllTweaksTests: 8 tests — definition verification for all 7 tweaks + JSON catalog loading
  - BatchTweakTests: 4 tests — batch apply all 7, apply-then-revert, HAGS path verification, Mouse Acceleration multi-value verification

### Grep Gates

| Gate | Expected | Actual | Status |
|------|----------|--------|--------|
| RegistryRights in src/ | 0 | 0 | PASS |
| RegistryView.Registry64 in src/ | >=1 | 20 | PASS |
| OpenSubKey(path, true) in src/Registry/ | >=4 | 8 | PASS |
| Wow6432Node in actual code | 0 | 0 (in comments only) | PASS |

### Resolved Ambiguities
- REG-04 (Win32PrioritySeparation): PLAN.md had ambiguous enabled/disabled values; resolved using REQUIREMENTS.md: enabled=26, disabled=38
- REG-01 (Game Mode): Used `GameModeEnabled` key name matching standard Windows Game Mode registry convention per PLAN.md truths section
- REG-03 (NetworkThrottlingIndex): 0xFFFFFFFF requires `unchecked` cast — used `uint.Parse` → `int` conversion

## Notes on Test-Only Validation
- All tests use `FakeRegistryProvider` (in-memory, logic-only) — cannot catch ACL failures
- Runtime ACL verification requires elevated app launch + log check at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log`
- See PITFALLS.md Pitfall 2 (D-04) for FakeRegistryProvider limitations
