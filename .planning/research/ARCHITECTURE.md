# Architecture Patterns: Akari Tool V2

**Domain:** Windows 11 gaming optimization toolbox
**Researched:** 2026-08-29

## Recommended Architecture

**Clean Layered Architecture** with strict separation between tweak definitions (Core), execution logic (Infrastructure), and UI (Presentation). Inspired by Microsoft's "Architecture patterns for WinUI 3 desktop apps" and the WinAurex reference implementation.

```
Akari.App (Presentation) → Akari.Core ← Akari.Infrastructure
```

The Core defines interfaces and tweak definitions (data). The Infrastructure provides concrete executors that implement those interfaces. The App layer composes them via DI and exposes them through a modular checklist UI.

### Component Boundaries

| Component | Responsibility | Communicates With |
|-----------|---------------|-------------------|
| **TweakCatalog** (Core) | Defines all tweak definitions as data (ID, name, description, category, registry path, expected value, revert value) | Infrastructure executors, App ViewModels |
| **ITweakEngine** (Core interface) | Dispatches a tweak to the correct executor based on its type | App ViewModels → Infrastructure executors |
| **ITweakExecutor** (Core interface) | Abstract execution: Apply(tweak) / Revert(tweak) | Concrete executors |
| **RegistryOperationExecutor** (Infrastructure) | Reads/writes registry keys for registry-based tweaks | Microsoft.Win32.Registry |
| **ServiceOperationExecutor** (Infrastructure) | Starts/stops Windows services (Xbox, GameDVR, etc.) | System.ServiceController |
| **ProcessOperationExecutor** (Infrastructure) | Sets process priority, terminates background processes | System.Diagnostics.Process |
| **PowerOperationExecutor** (Infrastructure) | Activates power schemes, sets power setting overrides | powercfg.exe via Process.Start |
| **NetworkOperationExecutor** (Infrastructure) | Sets NetworkThrottlingIndex, QoS policies | Microsoft.Win32.Registry, PowerShell |
| **IRegistryProvider** (Core interface) | Abstraction over RegistryKey access | RegistryOperationExecutor |
| **Win32RegistryProvider** (Infrastructure) | Concrete RegistryKey implementation using 2-arg OpenSubKey(path, true) | Microsoft.Win32.Registry |
| **FakeRegistryProvider** (Tests) | In-memory dict, no ACL enforcement | Unit tests |
| **ITweakStateService** (Core interface) | Persists which tweaks are currently applied | JSON file in %LOCALAPPDATA% |
| **TweakStateService** (Infrastructure) | JSON-based state persistence | File system |
| **ILogService** (Core interface) | Structured logging | File system |
| **FileLogService** (Infrastructure) | Logs to %LOCALAPPDATA%\Akari\App\logs\ | File system |
| **MainViewModel** (App) | Exposes tweak categories as observable collections | TweakCatalog, ITweakEngine, ITweakStateService |
| **TweakCategoryViewModel** (App) | Per-category toggle state, applies/reverts group | ITweakEngine |
| **MainWindow** (App) | WinUI 3 NavigationView with modular checklist | ViewModels via DI |

## Data Flow

1. **On startup**: `App.xaml.cs` registers all Core interfaces → Infrastructure implementations in DI container
2. **MainViewModel** loads `TweakCatalog` (all tweak definitions) and `ITweakStateService` (current applied state)
3. **User toggles** a tweak in the UI → `TweakCategoryViewModel.Apply(tweak)` → `ITweakEngine.Apply(tweak)` → dispatches to correct `ITweakExecutor` based on tweak type
4. **Executor** calls `IRegistryProvider` (or ServiceController, Process, etc.) → writes to system → logs result via `ILogService`
5. **ITweakStateService** records the applied state → persists to JSON
6. **On revert**: same flow but calls `ITweakEngine.Revert(tweak)` → executor writes revert value → state service clears state

Key design: the tweak definition is pure data. The engine dispatches. The executor implements. This makes the system testable (FakeRegistryProvider) but requires runtime verification (ACL enforcement is real-world only).

## Build Order

1. **TweakCatalog data definitions** — define all tweaks as data structures (no logic)
2. **IRegistryProvider + Win32RegistryProvider** — the foundation abstraction (with the 2-arg OpenSubKey workaround baked in)
3. **ITweakEngine + ITweakExecutor + concrete executors** — dispatch logic (RegistryOperationExecutor, ServiceOperationExecutor, etc.)
4. **ITweakStateService + ILogService** — persistence and logging
5. **DI registration in App.xaml.cs** — compose the layers
6. **WinUI 3 Views/ViewModels** — modular checklist UI wired to engine
7. **Integration/testing** — real elevated runs + log verification

The registry engine must be built first because it's the dependency for most tweak types. The UI comes last since it's a consumer of the engine.

## Patterns to Follow

### Pattern 1: Strategy Pattern for Tweak Execution

Each tweak type (Registry, Service, Process, Power, Network) gets its own `ITweakExecutor` implementation. The `ITweakEngine` selects the correct executor based on the tweak's type enum.

```csharp
public enum TweakType { Registry, Service, Process, Power, Network, File }

public interface ITweakExecutor
{
    Task<bool> ApplyAsync(TweakDefinition tweak);
    Task<bool> RevertAsync(TweakDefinition tweak);
}
```

### Pattern 2: Provider Abstraction for Registry Access

```csharp
public interface IRegistryProvider
{
    string? GetValue(string keyPath, string valueName);
    void SetValue(string keyPath, string valueName, object value, RegistryValueKind kind);
    void DeleteValue(string keyPath, string valueName);
    bool KeyExists(string keyPath);
}
```

This allows `FakeRegistryProvider` for unit tests (in-memory dict, no ACL enforcement) and `Win32RegistryProvider` for real operations.

### Pattern 3: JSON Tweak Catalog

Tweaks are defined as JSON data, loaded at startup — not hard-coded in C#. This makes it easy to add/remove tweaks without recompiling.

## Anti-Patterns to Avoid

- **Hard-coded registry paths in ViewModels** — keep all tweak data in the catalog, not in UI logic
- **Direct RegistryKey usage in ViewModels** — always go through IRegistryProvider abstraction
- **Blocking UI during system operations** — all executor operations must be async
- **No state tracking** — always record what's applied so the UI reflects reality
- **Assuming FakeRegistryProvider tests catch ACL issues** — they don't; runtime verification is mandatory

## Sources

- Microsoft Learn — Architecture patterns for WinUI 3 desktop apps: https://learn.microsoft.com/en-us/windows/apps/develop/architecture-patterns
- WinAurex — Windows 11 Tweaks Guide (rollback engine reference): https://winaurex.vercel.app/tweaks/
- Microsoft Learn — MMCSS (Multimedia Class Scheduler Service): https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
- Microsoft Docs — RegistryKey.OpenSubKey: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.opensubkey
- Stack Overflow — RegistryKey.SetValue UnauthorizedAccessException: https://stackoverflow.com/questions/11768172/c-sharp-registry-setvalue-throws-unauthorizedaccessexception
- Microsoft Docs — QoS Policy: https://learn.microsoft.com/en-us/windows-server/networking/technologies/qos/qos-policy-top
- Microsoft Learn — Get-NetQosPolicy: https://learn.microsoft.com/en-us/powershell/module/netqos/get-netqospolicy
- Microsoft Docs — Fullscreen Optimizations registry: https://learn.microsoft.com/en-us/answers/questions/3741077/fullscreen-optimizations-windows-registry
