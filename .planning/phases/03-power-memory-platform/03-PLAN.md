# Phase 3 — PLAN.md (Orchestration)

## Phase Goal
Add power plan management (Ultimate Performance with High Performance fallback)
and Windows memory compression toggle on top of the engine from Phase 1/2.

## Depends On
Phase 1 (engine), Phase 2 (service/process patterns)

## Requirements
PWR-01, PWR-02, MEM-01

## Wave Plan

### Wave 1: Power Operation (03-01-PLAN.md)
**03-01**: PowerOperationExecutor + PWR-01/PWR-02 tweak classes

Files:
- `src/Akari.Engine/Power/IPowerManager.cs` — abstraction over powercfg.exe
- `src/Akari.Engine/Power/FakePowerManager.cs` — in-memory fake for tests
- `src/Akari.Engine/Power/PowerManager.cs` — production implementation
- `src/Akari.Engine/Tweaks/PowerOperationExecutor.cs` — ITweakExecutor for TweakType.Power
- `src/Akari.Engine/Tweaks/Power/UltimatePerformanceTweak.cs` — PWR-01
- `src/Akari.Engine/Tweaks/Power/HighPerformanceTweak.cs` — PWR-02 (fallback)
- `src/Akari.Engine.Tests/PowerOperationTests.cs` — 7 tests

Success Criteria:
- PWR-01: Ultimate Performance activates via powercfg /duplicatescheme + /SETACTIVE
- PWR-02: High Performance fallback when Ultimate Performance GUID not found (Pitfall 9)
- Power operations logged to app log file
- FakePowerManager tests are logic-only (Pitfall 2)

### Wave 2: Memory Operation (03-02-PLAN.md)
**03-02**: MemoryOperationExecutor + MEM-01 tweak class

Files:
- `src/Akari.Engine/Memory/IMemoryManager.cs` — abstraction over MMAgent/PowerShell
- `src/Akari.Engine/Memory/FakeMemoryManager.cs` — in-memory fake for tests
- `src/Akari.Engine/Memory/MemoryManager.cs` — production implementation
- `src/Akari.Engine/Tweaks/MemoryOperationExecutor.cs` — ITweakExecutor for TweakType.Memory
- `src/Akari.Engine/Tweaks/Memory/MemoryCompressionTweak.cs` — MEM-01
- `src/Akari.Engine.Tests/MemoryOperationTests.cs` — 6 tests

Success Criteria:
- MEM-01: Memory compression toggled via Disable-MMAgent/Enable-MMAgent
- PowerShell invocation captures output for verification
- Memory operations logged to app log file
- FakeMemoryManager tests are logic-only (Pitfall 2)

## Wave Dependencies
- 03-01 → 03-02 (independent, can run in parallel)

## Verification
- `dotnet build src/Akari.Engine/ && dotnet build src/Akari.Engine.Tests/` — 0 errors, 0 warnings
- `dotnet test src/Akari.Engine.Tests/` — all tests pass (expect 60+ total)
