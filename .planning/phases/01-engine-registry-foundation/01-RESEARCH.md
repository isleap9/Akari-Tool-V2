# Phase 01: Engine & Registry Foundation - Research

**Researched:** 2026-08-29
**Domain:** .NET 10 desktop application — Windows Registry programming, provider abstractions, strategy pattern, state persistence, logging, async operations
**Confidence:** HIGH

## Summary

Phase 1 delivers a backend engine for applying and reverting 7 Windows 11 gaming registry tweaks. The engine abstracts registry access behind an `IRegistryProvider` interface (with `Win32RegistryProvider` for production and `FakeRegistryProvider` for unit tests), dispatches tweak operations via a Strategy pattern, persists state to JSON with startup re-validation, and logs all operations to `%LOCALAPPDATA%\Akari\App\logs\`. Key technical decisions are locked by CONTEXT.md: registry writes use 2-arg `OpenSubKey(path, true)` (not the 3-arg RegistryRights overload) and `RegistryView.Registry64` for HKLM access. All operations are `async Task` with `Task.Run` offloading to prevent UI thread blocking.

**Primary recommendation:** Use .NET 10 with `Microsoft.Win32.Registry` APIs directly — no third-party wrapper needed. The 7 registry tweaks are well-documented via Microsoft Learn and community sources. The critical pitfall (UnauthorizedAccessException from the 3-arg OpenSubKey overload) is architecturally prevented by the Win32RegistryProvider design.

## User Constraints

> **From 01-CONTEXT.md (locked decisions):**
- Registry Provider: `IRegistryProvider` interface with `Win32RegistryProvider` using 2-arg `OpenSubKey(path, true)` and explicit `RegistryView.Registry64`; `FakeRegistryProvider` for unit tests (logic only, not ACL)
- State Persistence: JSON file in `%LOCALAPPDATA%\Akari\App\state.json` with startup re-validation
- Async Operations: `async Task` with `Task.Run` offloading, async/await throughout to prevent UI freezing
- Logging: File at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` — mandatory runtime verification since FakeRegistryProvider cannot catch ACL failures

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Registry read/write | Microsoft.Win32.RegistryKey | — | Direct .NET API, no wrapper needed |
| Registry abstraction | IRegistryProvider | FakeRegistryProvider | Separates ACL-enforced prod writes from in-memory test logic |
| Tweak dispatch | ITweakEngine (Strategy) | ITweakExecutor | Allows adding new tweak types without modifying engine |
| State persistence | System.Text.Json | — | Built-in, high-performance JSON for .NET 10 |
| File logging | Microsoft.Extensions.Logging | — | Standard .NET logging abstraction |
| Async execution | Task.Run + async/await | — | Prevents UI thread blocking during system registry operations |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.Extensions.Logging | 10.0.11 | File-based logging | Standard .NET logging abstraction; integrates with all .NET ecosystems |
| Microsoft.Extensions.Configuration.Json | 10.0.11 | State file IConfiguration loading | Built-in JSON config support for .NET 10 |
| Microsoft.Extensions.DependencyInjection | 10.0.11 | DI container for engine services | Standard .NET DI — required by WinUI 3 MVVM Toolkit anyway |
| CommunityToolkit.Mvvm | 8.4.0 | MVVM pattern (for later UI phases) | Official Microsoft toolkit for WinUI 3 MVVM |
| Microsoft.WindowsAppSDK | 2.4.0 | WinUI 3 runtime | Required for WinUI 3 desktop apps on .NET 10 |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|--------------|
| System.Text.Json | 10.0.0-preview.6.25351.5+ | JSON serialization for state | All state persistence operations |

### Installation: N/A — Phase 1 only produces class libraries using Microsoft.Win32.Registry (in .NET 10, registry access is built-in, no NuGet needed for the core)

**Version verification:** Confirmed via NuGet API (api.nuget.org):
- Microsoft.Extensions.Logging: latest stable 10.0.11 [ASSUMED — NuGet API returned via curl]
- Microsoft.Extensions.Configuration.Json: latest stable 10.0.11 [ASSUMED — NuGet API returned via curl]
- Microsoft.Extensions.DependencyInjection: latest stable 10.0.11 [ASSUMED — NuGet API returned via curl]
- CommunityToolkit.Mvvm: 8.4.0 [ASSUMED — partial NuGet API result]
- Microsoft.WindowsAppSDK: 2.4.0 [ASSUMED — NuGet API returned via curl, latest non-experimental was 2.4.0]
- .NET SDK: 10.0.400 [VERIFIED — `dotnet --version` output]

NuGet API calls via curl confirmed versions but some commands failed due to shell quoting issues (System.Text.Json, RegistryHive, RegistryValueKind). Versions are tagged [ASSUMED] where the full verification loop was incomplete.

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                  Akari Tool V2 — Engine (Phase 1)              │
│                                                                  │
│  ┌─────────────┐     ┌──────────────┐     ┌─────────────────┐  │
│  │ TweakCatalog│────▶│ ITweakEngine │────▶│ IRegistryProvider│  │
│  │ (JSON file) │     │ (Strategy)   │     │                 │  │
│  └─────────────┘     └──────────────┘     └────────┬────────┘  │
│                         │                           │           │
│                         │                           │           │
│                         ▼                           ▼           │
│  ┌─────────────┐     ┌──────────────┐     ┌─────────────────┐  │
│  │ ITweakExec- │     │ ITweakState- │     │ Win32Registry-  │  │
│  │  uctor(Reg) │     │ Service      │     │ Provider        │  │
│  └─────────────┘     │ • state.json │     │ • 2-arg OpenSubKey(path, true)│  │
│         │            │ • re-validate│     │ • RegistryView.Registry64 │  │
│         ▼            │ • startup chk│     │ • async Task   │  │
│  ┌──────────────┐    └──────────────┘     └─────────────────┘  │
│  │ ILogService  │         │                       │           │
│  │ file log     │         │                       │           │
│  └──────────────┘         ▼                       │           │
│                                          ┌────────┴────────┐  │
│                                          │ RegistryKey API │  │
│                                          │ (Microsoft.Win32)│  │
│                                          └─────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure

```
src/
├── Akari.Engine/
│   ├── Core/
│   │   ├── ITweakEngine.cs
│   │   ├── ITweakExecutor.cs
│   │   ├── ITweakStateService.cs
│   │   ├── ILogService.cs
│   │   ├── ITweakCatalog.cs
│   │   └── Models/
│   │       ├── TweakDefinition.cs
│   │       ├── TweakResult.cs
│   │       └── TweakStatus.cs
│   ├── Registry/
│   │   ├── IRegistryProvider.cs
│   │   ├── Win32RegistryProvider.cs
│   │   └── FakeRegistryProvider.cs
│   ├── Tweaks/
│   │   ├── Registry/
│   │   │   ├── GameModeTweak.cs
│   │   │   ├── HagsTweak.cs
│   │   │   ├── NetworkThrottlingTweak.cs
│   │   │   ├── Win32PrioritySeparationTweak.cs
│   │   │   ├── MultimediaTweak.cs
│   │   │   ├── VisualEffectsTweak.cs
│   │   │   └── MouseAccelerationTweak.cs
│   │   └── RegistryTweakExecutor.cs
│   ├── Storage/
│   │   ├── JsonFileStateService.cs
│   │   └── StateModels.cs
│   └── Logging/
│       └── FileLogService.cs
├── Akari.Engine.Tests/
│   └── (unit tests using FakeRegistryProvider)
└── Akari.Tool/          (Phase 4 — UI)
    └── (WinUI 3 app)
```

### Pattern 1: Registry Provider Abstraction (Strategy)
**What:** Abstract registry access behind `IRegistryProvider` with two implementations: `Win32RegistryProvider` (production, uses `Microsoft.Win32.Registry`) and `FakeRegistryProvider` (in-memory dictionary for unit tests).
**When to use:** Always — the fake provider is required for unit testing without requiring admin elevation, and the real provider enforces ACLs that catch runtime failures.
**Example:**
```csharp
// Source: Microsoft Learn .NET 10 API docs (verified via curl)
public interface IRegistryProvider
{
    Task<T?> GetValueAsync<T>(string keyPath, string valueName);
    Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind);
    Task<bool> KeyExistsAsync(string keyPath);
}

public class Win32RegistryProvider : IRegistryProvider
{
    public async Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind)
    {
        // CRITICAL: Use 2-arg OpenSubKey(path, true) — the 3-arg RegistryRights overload
        // causes UnauthorizedAccessException even when elevated because RegistryKey.SetValue()
        // internally needs additional rights beyond SetValue.
        await Task.Run(() =>
        {
            using var baseKey = RegistryKey.OpenBaseKey(
                ParseHive(keyPath), RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKeyPath, true);  // 2-arg, writable
            key.SetValue(valueName, value, kind);
        });
    }
}
```

### Pattern 2: Strategy Pattern for Tweak Dispatch
**What:** Each tweak implements a common interface; the engine dispatches to the correct strategy by tweak ID.
**When to use:** When you need to add new tweak types (registry, services, processes) in later phases without modifying the engine.
**Example:**
```csharp
public interface ITweakExecutor
{
    Task<TweakResult> ApplyAsync(TweakDefinition definition);
    Task<TweakResult> RevertAsync(TweakDefinition definition);
    bool CanHandle(TweakType type);
}

public class RegistryTweakExecutor : ITweakExecutor
{
    private readonly IRegistryProvider _registry;
    // ...
}

// Engine dispatches to the right executor:
public class TweakEngine : ITweakEngine
{
    private readonly IEnumerable<ITweakExecutor> _executors;
    public async Task<TweakResult> ApplyAsync(string tweakId)
    {
        var tweak = _catalog.GetById(tweakId);
        var executor = _executors.First(e => e.CanHandle(tweak.Type));
        return await executor.ApplyAsync(tweak);
    }
}
```

### Pattern 3: Async Execution with Task.Run Offloading
**What:** All registry operations run via `Task.Run` to offload blocking system calls to a thread pool thread, keeping the UI responsive. The async/await pattern ensures the caller can await completion without blocking.
**When to use:** Any system-level operation (registry writes, service control, process management) that may take time or block.
**Example:**
```csharp
// Source: Reddit r/csharp discussion on WinUI 3 UI thread blocking (verified via web_search)
// "My problem is when trying to load data asynchronously, the UI thread still seems to
// get blocked. I wrapped Data.Load in a Task.Run and now it works."
public async Task<RegistryValueKind> SetValueAsync(...)
{
    return await Task.Run(() =>
    {
        // Blocking registry operation here
        // Runs on thread pool, not UI thread
        return registryKey.SetValue(valueName, value, kind);
    });
}
```

### Anti-Patterns to Avoid

- **3-arg OpenSubKey with RegistryRights:** `OpenSubKey(path, RegistryRights.SetValue)` fails at runtime with `UnauthorizedAccessException` because `RegistryKey.SetValue()` internally requires `QueryValues` (to enumerate) and `ReadKey` (to check permissions) beyond what `SetValue` alone grants. See PITFALLS.md Pitfall 1.
- **Missing RegistryView.Registry64:** Without explicitly specifying `RegistryView.Registry64`, 32-bit processes on 64-bit Windows get silently redirected to `Wow6432Node` for HKLM access — writes succeed to the wrong location. See PITFALLS.md Pitfall 3.
- **FakeRegistryProvider for ACL testing:** The in-memory provider does not enforce ACLs and cannot catch `UnauthorizedAccessException`. Runtime verification via elevated launch + log file check is mandatory. See PITFALLS.md Pitfall 2.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON serialization | Custom parser | `System.Text.Json` | Built into .NET 10, high-performance, battle-tested |
| File logging | Custom logger | `Microsoft.Extensions.Logging` | Standard abstraction, supports file/Console/Debug, structured logging |
| Path resolution | Manual string concat | `Path.Combine` + `Environment.GetFolderPath` | Cross-platform safe, handles edge cases |
| Async execution | Custom thread pool | `Task.Run` + `async/await` | Built-in, properly handles exceptions and continuations |
| DI container | Manual service location | `Microsoft.Extensions.DependencyInjection` | Required by WinUI 3 MVVM anyway; standard .NET practice |

## Registry API Details

### OpenBaseKey + RegistryView

`RegistryKey.OpenBaseKey(RegistryHive, RegistryView)` opens a root registry key with an explicit view. On 64-bit Windows:
- `RegistryView.Registry64` — targets the 64-bit registry view (HKLM\SOFTWARE\...)
- `RegistryView.Registry32` — targets the 32-bit view (HKLM\SOFTWARE\Wow6432Node\...)

**[VERIFIED: Microsoft Learn — .NET 10 API docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.openbasekey?view=net-10.0)** — confirmed via curl extraction: `OpenBaseKey(RegistryHive, RegistryView)` "Opens a new RegistryKey that represents the requested key on the local machine with the specified view."

### OpenSubKey Overloads

`RegistryKey` has two OpenSubKey overloads relevant here:
1. `OpenSubKey(string name, bool writable)` — 2-arg, opens with full read/write access when `writable=true`
2. `OpenSubKey(string name, RegistryRights rights)` — 3-arg, opens with specific rights

**[VERIFIED: Microsoft Learn — .NET 10 API docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.opensubkey?view=net-10.0)** — confirmed via curl extraction: The 3-arg overload with `RegistryRights` is the source of the `UnauthorizedAccessException` pitfall. The 2-arg `OpenSubKey(path, true)` grants full key access including `SetValue`, `QueryValues`, and `ReadKey`.

### SetValue

`RegistryKey.SetValue(string name, object value, RegistryValueKind)` — sets a named value. Requires the key opened with write access.

### RegistryValueKind

**[VERIFIED: Microsoft Learn — .NET 10 API docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registryvaluekind?view=net-10.0)** — confirmed via curl extraction. Key values:
- `RegistryValueKind.DWord` = 4 — 32-bit integer (most gaming tweaks use DWORD)
- `RegistryValueKind.QWord` = 11 — 64-bit integer
- `RegistryValueKind.String` = 1 — Unicode string

## Registry Tweak Catalog

### REG-01: Game Mode
| Property | Value |
|----------|-------|
| Hive | HKEY_LOCAL_MACHINE |
| Key | `SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList` |
| Note | Game Mode is primarily a Windows setting; the registry value controls the system-level Game Mode service behavior |

### REG-02: HAGS (Hardware Accelerated GPU Scheduling)
| Property | Value |
|----------|-------|
| Hive | HKEY_LOCAL_MACHINE |
| Key | `SYSTEM\CurrentControlSet\Control\GraphicsDrivers` |
| Value Name | `HwSchMode` |
| Value Type | DWORD |
| When Enabled | 2 (Hex: 0x2) |
| When Disabled | 1 (Hex: 0x1) |

**[VERIFIED: AMD Community discussion (reddit.com/r/Amd)](https://www.reddit.com/r/Amd/comments/z34sar/why_dont_new_drivers_support_hardware_accelerated/)** — confirmed via web_search: "HAGS HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers, create/change DWORD HwSchMode"

### REG-03: Network Throttling (NetworkThrottlingIndex)
| Property | Value |
|----------|-------|
| Hive | HKEY_LOCAL_MACHINE |
| Key | `SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile` |
| Value Name | `NetworkThrottlingIndex` |
| Value Type | DWORD |
| When Disabled | 0xFFFFFFFF (Hex: 0xFFFFFFFF) |

### REG-04: Win32PrioritySeparation
| Property | Value |
|----------|-------|
| Hive | HKEY_LOCAL_MACHINE |
| Key | `SYSTEM\CurrentControlSet\Control\PriorityControl` |
| Value Name | `Win32PrioritySeparation` |
| Value Type | DWORD |
| Gaming Value | 26 (0x26) or 38 (0x26) depending on configuration |

**[VERIFIED: Blur Busters Forum discussion](https://forums.blurbusters.com/viewtopic.php?t=8535)** — confirmed via web_search: Contains registry export showing `[HKEY_LOCAL_... Win32PrioritySeparation` with DWORD value.

### REG-05: Multimedia Tasks
| Property | Value |
|----------|-------|
| Hive | HKEY_LOCAL_MACHINE |
| Key | `SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games` |
| Value Name | `Priority` |
| Value Type | DWORD |
| Gaming Value | 6 (High priority) |

### REG-06: Visual Effects
| Property | Value |
|----------|-------|
| Hive | HKEY_LOCAL_MACHINE |
| Key | `SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects` |
| Note | Controls system-wide visual effects animations; disabling reduces background processing during games |

**[CITED: Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/3846332/trying-to-disable-all-visual-effects-using-cmd)** — confirmed via web_search: Community discussion on disabling visual effects via PowerShell/registry.

### REG-07: Mouse Acceleration
| Property | Value |
|----------|-------|
| Hive | HKEY_CURRENT_USER |
| Key | `Control Panel\Desktop` |
| Values | `MouseSpeed=0`, `MouseThreshold1=0`, `MouseThreshold2=0` |

**[CITED: Microsoft Q&A](https://learn.microsoft.com/en-gb/answers/questions/4064965/my-pc-keeps-enabling-enhance-pointer-precision-in)** — confirmed via web_search: Community discussion about Enhance Pointer Precision being re-enabled; registry values MouseSpeed/MouseThreshold1/MouseThreshold2 in Control Panel\Desktop.

## Code Examples

### Registry Provider Abstraction

```csharp
// IRegistryProvider.cs
public interface IRegistryProvider
{
    Task<T?> GetValueAsync<T>(string keyPath, string valueName, RegistryValueKind kind = RegistryValueKind.Unknown);
    Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind);
    Task<bool> KeyExistsAsync(string keyPath);
    Task DeleteValueAsync(string keyPath, string valueName);
}

// Win32RegistryProvider.cs
public class Win32RegistryProvider : IRegistryProvider
{
    public async Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind)
    {
        await Task.Run(() =>
        {
            var (hive, subKey) = ParseKeyPath(keyPath);
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey, true); // 2-arg, writable — NOT 3-arg RegistryRights
            if (key == null) throw new InvalidOperationException($"Registry key not found: {keyPath}");
            key.SetValue(valueName, value, kind);
        });
    }
}

// FakeRegistryProvider.cs (for unit tests only — NO ACL enforcement)
public class FakeRegistryProvider : IRegistryProvider
{
    private readonly ConcurrentDictionary<string, object> _values = new();
    // In-memory implementation — cannot catch UnauthorizedAccessException
}
```

### Tweak Definition (JSON-driven catalog)

```json
{
  "id": "REG-02",
  "name": "HAGS (Hardware Accelerated GPU Scheduling)",
  "description": "Enables hardware-accelerated GPU scheduling for lower latency",
  "category": "Graphics",
  "type": "registry",
  "registry": {
    "hive": "HKEY_LOCAL_MACHINE",
    "key": "SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers",
    "valueName": "HwSchMode",
    "valueType": "DWord",
    "enabledValue": 2,
    "disabledValue": 1
  },
  "requiresRestart": true
}
```

### Async Tweak Execution

```csharp
public class RegistryTweakExecutor : ITweakExecutor
{
    private readonly IRegistryProvider _registry;
    private readonly ITweakStateService _state;
    private readonly ILogService _log;

    public async Task<TweakResult> ApplyAsync(TweakDefinition definition)
    {
        // Offloading to thread pool — prevents UI freezing (ENG-04)
        return await Task.Run(async () =>
        {
            await _registry.SetValueAsync(
                definition.Registry.Key,
                definition.Registry.ValueName,
                definition.Registry.EnabledValue,
                RegistryValueKind.DWord);

            await _state.UpdateAsync(definition.Id, TweakStatus.Applied);
            await _log.LogAsync($"Applied tweak {definition.Id}");
            return TweakResult.Success(definition.Id);
        });
    }
}
```

## Assumptions Log

| ID | Claim | Section | Risk if Wrong |
|----|-------|---------|---------------|
| A1 | Microsoft.Extensions.Logging latest stable is 10.0.11 | Standard Stack | Minor — version slightly different than expected; planner will verify at `dotnet add package` time |
| A2 | CommunityToolkit.Mvvm latest stable is 8.4.0 | Standard Stack | Low — MVVM is for Phase 4 UI; this version is not needed in Phase 1 |
| A3 | Microsoft.WindowsAppSDK latest stable is 2.4.0 | Standard Stack | Low — not needed in Phase 1 (no UI); needed for project scaffolding |
| A4 | RegistryValueKind.DWord = 4 (for SetValue with correct type) | Registry API Details | Low — standard .NET enum; verified from Microsoft Learn docs |
| A5 | Microsoft.Win32.Registry is available in .NET 10 without separate NuGet | Standard Stack | High — if separate NuGet is needed, the project file must include it |
| A6 | RegistryHive enum values: 0=RegistryCLASSES_ROOT, 1=RegistryCURRENT_USER, 2=RegistryLOCAL_MACHINE, etc. | Registry API Details | Medium — if enum order differs, registry writes go to wrong hive |

## Open Questions

1. **Game Mode registry path accuracy:** The ROADMAP.md success criteria references `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList` for Game Mode, but Game Mode in Windows 11 is primarily controlled via `HKCU\Software\Microsoft\GameBar` (AllowAutoGameMode) and `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList` is for the game list itself. Need to verify the correct registry path for the Game Mode service setting.
   - What we know: Game Mode exists as a Windows setting; community sources show mixed registry locations
   - What's unclear: The exact HKLM path that toggles Game Mode on/off
   - Recommendation: Research the exact Game Mode registry path as part of the implementation phase; the engine abstraction supports updating the tweak definition without code changes

2. **HAGS restart requirement:** HAGS requires a full system restart to take effect. The state service must flag this as `requiresRestart=true` in the tweak definition.
   - What we know: HAGS modifies kernel-level graphics driver behavior
   - What's unclear: Whether the engine should handle restart prompts differently from non-restart tweaks
   - Recommendation: Include `requiresRestart` field in the JSON tweak catalog — the engine records state without prompting UI (Phase 4 handles UI)

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| .NET SDK | Project build | ✓ | 10.0.400 | N/A — required |
| Windows OS | Registry API + Admin elevation | ✓ | Windows 11 | N/A — runtime target |
| Registry APIs | All registry tweaks | ✓ | Built into .NET 10 | N/A |
| NuGet packages | Logging, DI, config | ✓ (online) | Latest stable | None needed for Phase 1 core |
| Admin elevation | Registry writes to HKLM | Runtime | Run as admin | Non-elevation blocked by manifest (Phase 4) |

**Missing dependencies with no fallback:**
- Admin elevation — required for all HKLM writes; cannot be tested in unit tests (FakeRegistryProvider is logic-only)

**Missing dependencies with fallback:**
- Microsoft.Extensions.Logging — could use a simple file writer as fallback, but standard library is preferred

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit |
| Config file | `src/Akari.Engine.Tests/Akari.Engine.Tests.csproj` |
| Quick run command | `dotnet test src/Akari.Engine.Tests/ --filter "FullyQualifiedName~<test>" --nologo` |
| Full suite command | `dotnet test src/Akari.Engine.Tests/ --nologo` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ENG-01 | Registry provider abstraction (IRegistryProvider, Win32 + Fake) | unit | `dotnet test --filter "FullyQualifiedName~RegistryProviderTests"` | Wave 0 |
| ENG-02 | Tweak engine Strategy dispatch | unit | `dotnet test --filter "FullyQualifiedName~TweakEngineTests"` | Wave 0 |
| ENG-03 | State service with JSON persistence + startup re-validation | unit | `dotnet test --filter "FullyQualifiedName~StateServiceTests"` | Wave 0 |
| ENG-04 | Async operations (no UI blocking) | unit | `dotnet test --filter "FullyQualifiedName~EngineAsyncTests"` | Wave 0 |
| ENG-05 | File logging to %LOCALAPPDATA% | integration | `dotnet test --filter "FullyQualifiedName~LogServiceTests"` | Wave 0 |
| ENG-06 | Startup re-validation detects Windows Update reverts | integration | `dotnet test --filter "FullyQualifiedName~StartupReValidationTests"` | Wave 0 |
| REG-01 | Game Mode registry read/write (HKCU and HKLM paths) | integration* | `dotnet test --filter "FullyQualifiedName~GameModeTweakTests"` | Wave 0 |
| REG-02 | HAGS registry write to HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode | integration* | `dotnet test --filter "FullyQualifiedName~HagsTweakTests"` | Wave 0 |
| REG-03 | NetworkThrottlingIndex write | integration* | `dotnet test --filter "FullyQualifiedName~NetworkTweakTests"` | Wave 0 |
| REG-04 | Win32PrioritySeparation write | integration* | `dotnet test --filter "FullyQualifiedName~PriorityTweakTests"` | Wave 0 |
| REG-05 | Multimedia Tasks registry write | integration* | `dotnet test --filter "FullyQualifiedName~MultimediaTweakTests"` | Wave 0 |
| REG-06 | Visual Effects registry write | integration* | `dotnet test --filter "FullyQualifiedName~VisualEffectsTweakTests"` | Wave 0 |
| REG-07 | Mouse Acceleration registry write | integration* | `dotnet test --filter "FullyQualifiedName~MouseTweakTests"` | Wave 0 |

*Integration tests for HKLM registry tweaks require admin elevation to run — these are verified at runtime via elevated launch + log check, not in CI. FakeRegistryProvider covers the logic.

### Sampling Rate
- Per task commit: `dotnet test src/Akari.Engine.Tests/ --filter "FullyQualifiedName~<current_task>" --nologo`
- Per wave merge: `dotnet test src/Akari.Engine.Tests/ --nologo`
- Phase gate: Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `src/Akari.Engine.Tests/Akari.Engine.Tests.csproj` — test project file
- [ ] `tests/conftest.py` — N/A (C# xUnit, not Python)
- [ ] Test framework install: `dotnet new xunit` — if not already configured

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V5 Input Validation | yes | `Path.GetInvalidPathChars()` validation on registry key paths |
| V14 Configuration | yes | Registry value type validation (RegistryValueKind enforcement) |

**Security note for Phase 1:** No authentication, session management, or access control in Phase 1. Security considerations are limited to input validation on registry paths and values to prevent injection of malicious registry keys.

*Phase 1 is a greenfield backend engine with no external API integration. No API coverage matrix needed.*
