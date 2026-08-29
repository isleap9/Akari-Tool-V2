# Phase 3 CONTEXT — Power, Memory & Platform Tweaks

## Phase 1 Status: Complete
- Registry provider abstraction (2-arg OpenSubKey, RegistryView.Registry64)
- Tweak engine dispatch (Strategy pattern)
- State service (JSON persistence + startup re-validation)
- Logging to %LOCALAPPDATA%\Akari\App\logs\
- 7 registry tweaks (REG-01 through REG-07)
- 33/33 Phase 1 tests pass

## Phase 2 Status: Complete
- ServiceOperationExecutor + IServiceControllerFactory/FakeServiceControllerFactory
- ProcessOperationExecutor + IProcessManager/FakeProcessManager
- SVC-01 (Xbox services), SVC-02 (GameDVR/GameBar), PROC-01 (process priority), PROC-02 (kill)
- 47/47 tests pass (33 Phase 1 + 14 Phase 2)

## Phase 3 Requirements

### PWR-01: Activate Ultimate Performance Power Plan
**PowerShell source:** AkariOS Tweaks/6 Windows/29 Power Plan.ps1, lines 21-25:
```
cmd /c "powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 99999999-9999-9999-9999-999999999999 >nul 2>&1"
cmd /c "powercfg /SETACTIVE 99999999-9999-9999-9999-999999999999 >nul 2>&1"
```
- Ultimate Performance base GUID: `e9a42b02-d5df-448d-aa00-03f14749eb61`
- Target scheme GUID: `99999999-9999-9999-9999-999999999999`
- Uses `powercfg /duplicatescheme` to create a copy of Ultimate Performance
- Then `/SETACTIVE` to activate it
- Requires admin elevation

### PWR-02: Activate High Performance Power Plan (Fallback)
**PowerShell source:** Power Plan.ps1 revert path + standard Windows power plans
- High Performance GUID: `8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c`
- Fallback when Ultimate Performance GUID is not found (Pitfall #9 — GUID confusion)
- Uses `powercfg /SETACTIVE <GUID>`

### MEM-01: Toggle Windows Memory Compression
**PowerShell source:** AkariOS Tweaks/3 Setup/2 Memory Compression.ps1:
- Option 1 (Disable): `Disable-MMAgent -MemoryCompression`
- Option 2 (Enable): `Enable-MMAgent -MemoryCompression`
- Option 3 (Check): `get-mmagent`
- Uses PowerShell MMAgent module (not a simple registry tweak)
- Requires admin elevation
- May require reboot to take effect

## Architecture Decisions (Phase 3)

1. **PowerOperationExecutor** — Uses `Process.Start("powercfg.exe", ...)` to run powercfg commands
   - Must capture stdout/stderr for error detection
   - Async execution via Task.Run (Pitfall 5 — prevent UI blocking)
   - Fallback from Ultimate Performance (PWR-01) to High Performance (PWR-02) when GUID not found (Pitfall 9)
   
2. **MemoryOperationExecutor** — Uses PowerShell invocation to run `Disable-MMAgent` / `Enable-MMAgent`
   - PowerShell command: `powershell -Command "Disable-MMAgent -MemoryCompression"`
   - Captures output for verification
   - Async execution via Task.Run
   
3. **New TweakType values** — Already exist: `Power` and `Memory` (added during Phase 1 TweakDefinition extension)
   
4. **New model fields on TweakDefinition** — Need to add:
   - `PowerSchemeGuid: string?` — GUID for power plan activation (PWR-01, PWR-02)
   - `PowerShellCommand: string?` — PowerShell command for memory compression toggle (MEM-01)

## PowerShell Script Analysis

### Power Plan.ps1 (273 lines)

**Apply path (choice 1, lines 18-231):**
- `powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 99999999-9999-9999-9999-999999999999`
- `powercfg /SETACTIVE 99999999-9999-9999-9999-999999999999`
- `powercfg /hibernate off`
- Registry: HibernateEnabled=0, HiberbootEnabled=0, PowerThrottlingOff=1
- Various `powercfg /setacvalueindex` and `/setdcvalueindex` calls
- `Start-Process powercfg.cpl` (opens settings UI)

**Revert path (choice 2, lines 235-271):**
- `powercfg -restoredefaultschemes`
- `powercfg /hibernate on`
- Registry deletes/restore: HibernateEnabled (delete), HiberbootEnabled=1, PowerThrottling (delete)
- `Start-Process powercfg.cpl`

### Memory Compression.ps1 (61 lines)
- Disable: `Disable-MMAgent -MemoryCompression`
- Enable: `Enable-MMAgent -MemoryCompression`
- Check: `get-mmagent`
- Requires admin

## Patterns to Reuse (from Phase 1/Phase 2)

- **Strategy pattern**: `ITweakExecutor` with `CanHandle(TweakType)`
- **Async Task with Task.Run**: All operations must be async (Pitfall 5)
- **ILogService**: All operations logged (T-04-02 mitigation)
- **ITweakStateService**: State tracking for revert
- **FakeX pattern**: FakePowerManager / FakePowerShellManager for testability
- **Exception handling**: try/catch with error logging via ILogService.LogErrorAsync

## Pitfall Considerations (from PITFALLS.md)

- **Pitfall 5 (UI freeze)**: All executor methods must be async Task
- **Pitfall 9 (GUID confusion)**: Power plan GUIDs must be validated — fall back to High Performance if Ultimate Performance not available
- **Pitfall 2 (FakeProvider false positives)**: Fake implementations are logic-only — runtime verification via elevated launch + log check
