# Domain Pitfalls: Akari Tool V2

**Domain:** Windows 11 gaming optimization toolbox
**Researched:** 2026-08-29

## Critical Pitfalls

### Pitfall 1: UnauthorizedAccessException on registry writes despite elevation

**What goes wrong:** The application is running as administrator but `RegistryKey.SetValue()` throws `UnauthorizedAccessException` when writing to HKLM.

**Why it happens:** Using the 3-arg `OpenSubKey(path, RegistryKeyPermissionCheck, RegistryRights)` overload with only `RegistryRights.SetValue` is insufficient — `RegistryKey.SetValue()` internally needs `QueryValues` (to enumerate) and `ReadKey` (to check permissions), which aren't granted by the 3-arg overload's security context.

**Consequences:** Registry tweaks silently fail at runtime; users think the tool works but their settings aren't applied.

**Prevention:** Always use the 2-arg `OpenSubKey(path, true)` (writable) overload. This grants full write access without the permission granularity issue.

**Detection:** Check the application log at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` for `UnauthorizedAccessException` traces during tweak application.

**Phase:** Phase 1 — Registry engine must handle this from the start.

### Pitfall 2: FakeRegistryProvider tests pass but runtime fails

**What goes wrong:** Unit tests using `FakeRegistryProvider` (in-memory dictionary) pass all tests, but the real application crashes at runtime with `UnauthorizedAccessException`.

**Why it happens:** `FakeRegistryProvider` does not enforce ACLs — it's just a dictionary with no security context. The real `Win32RegistryProvider` uses actual Windows registry security, which the fake cannot simulate.

**Consequences:** Bugs reach production because tests give false confidence. Users encounter crashes that were never caught in testing.

**Prevention:** Treat `FakeRegistryProvider` tests as logic verification only. Always validate registry writes via actual elevated app launch + log check. Add a runtime verification step that checks `%LOCALAPPDATA%\Akari\App\logs\` for errors after applying tweaks.

**Detection:** Missing from test coverage — the gap only shows at runtime. Review logs after every elevated run.

**Phase:** Phase 1 — Testing strategy must include runtime verification from the start.

### Pitfall 3: Wow6432Node / 32-bit vs 64-bit registry redirection

**What goes wrong:** Tweaks written to the wrong registry view (32-bit vs 64-bit) don't take effect for 64-bit games/processes.

**Why it happens:** On 64-bit Windows, 32-bit processes are transparently redirected to `HKLM\SOFTWARE\Wow6432Node`. Using `RegistryKey` without specifying `RegistryView.Registry64` writes to the 32-bit view, which 64-bit games don't read.

**Consequences:** Tweaks appear to apply successfully but have no effect on 64-bit games — the most common scenario.

**Prevention:** Always explicitly specify `RegistryView.Registry64` when opening HKLM keys. Use `RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)`.

**Detection:** Verify via `regedit` that the value appears under the correct hive, not under Wow6432Node.

**Phase:** Phase 1 — Registry engine must handle this from the start.

### Pitfall 4: Irreversible tweaks causing system instability

**What goes wrong:** Users apply aggressive tweaks (disabling critical services, removing system components) and their system becomes unstable or fails to boot properly.

**Why it happens:** No per-tweak rollback mechanism. User applies changes system-wide without system restore points. Some tweaks (service disabling, registry key deletion) are hard to reverse without backup knowledge.

**Consequences:** System crashes, games won't launch, Windows features break. Users must reinstall Windows or spend hours troubleshooting.

**Prevention:** The tool's design includes per-tweak revert (user chose "moderate" aggression). However:
- Always use SetValue, not DeleteValue, when possible (SetValue can be reverted by setting the original value)
- Record original values before applying (state service)
- Warn users to create a system restore point before applying
- Do NOT remove Appx packages that are system-critical (no Appx management in v1 per user decision)

**Detection:** UI shows a warning banner before applying system-level tweaks. Log original values.

**Phase:** Phase 1+ — Core engine and state service must capture original values.

## Moderate Pitfalls

### Pitfall 5: UI freezes during system operations

**What goes wrong:** The WinUI 3 UI hangs or becomes unresponsive while applying tweaks that involve registry writes, service control, or process management.

**Why it happens:** Synchronous (non-async) calls to system APIs on the UI thread. `RegistryKey.SetValue()`, `ServiceController.Start()`, etc. can take seconds under load.

**Prevention:** All executor operations must be `async Task`. Use `Task.Run` for blocking system API calls. The `ITweakEngine` exposes `ApplyAsync`/`RevertAsync`. UI bindings must not block.

**Detection:** UI remains responsive (clickable, not "Not Responding") during tweak application. Progress indicator shows activity.

**Phase:** Phase 1 — Engine and UI must be async from day one.

### Pitfall 6: Appx removal breaking Windows functionality

**What goes wrong:** Removing "bloatware" Appx packages that are actually dependencies of critical Windows features (Settings app, Windows Store, Security Center).

**Why it happens:** Users don't know which Appx packages are safe to remove. Chris Titus Tech's debloater has documented lists of safe vs unsafe packages to remove, but generic tools don't distinguish.

**Consequences:** Windows Update breaks, Security Center disappears, Settings app fails to launch, Windows Store apps stop working.

**Prevention:** The user explicitly chose NO Appx management in v1 (moderate tweaks only). If Appx removal is added later, use curated safe-package lists (Chris Titus Tech / WinAurex methodology) and NEVER remove system-critical packages like `Windows.Apollo`, `Windows.Client.WebExperience`, `Microsoft.Windows.SecHealthUI`.

**Detection:** N/A (not in scope for v1 — but flagged for v2+ planning).

**Phase:** Not applicable for v1. Documented as a v2+ risk.

### Pitfall 7: Windows updates reverting applied tweaks

**What goes wrong:** After a Windows Update, registry-based tweaks are reset to defaults, but the tool's UI shows them as still applied.

**Why it happens:** Windows Updates can override registry settings. The tool's state file (tracking what was applied) is not re-validated against the actual system state.

**Prevention:** On application startup, re-validate each tweak's current state against the system (read the registry value, check service status). Reconcile state file with actual system state. Show "needs re-apply" for tweaks that drifted.

**Detection:** Startup validation log shows mismatches between tracked state and actual registry values.

**Phase:** Phase 1 — State service must include re-validation logic.

## Minor Pitfalls

### Pitfall 8: Telemetry interference with tweak application

**What goes wrong:** Windows telemetry (Compatibility Assistant, Compatibility Update) reverts or blocks certain registry changes, especially for compatibility-related settings.

**Prevention:** Apply tweaks early in the session, before telemetry services fully initialize. Log any `ERROR_ACCESS_DENIED` from telemetry-related service interference.

### Pitfall 9: Power plan GUID confusion

**What goes wrong:** Power plan GUIDs vary between Windows builds. Hard-coding a GUID that doesn't exist on the user's system causes powercfg to error.

**Prevention:** Query available power schemes first (`powercfg /list`), then match by name or verify GUID exists before activating. Fall back gracefully.

### Pitfall 10: Service dependency chains

**What goes wrong:** Stopping one service cascades to stopping dependent services that the user didn't expect to lose.

**Prevention:** Before stopping a service, enumerate its dependencies and warn the user. Prefer disabling (setting Start=4) over stopping for background services.

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|-----------|
| Registry engine | UnauthorizedAccessException (3-arg vs 2-arg OpenSubKey) | Use 2-arg writable overload exclusively |
| Registry engine | Wow6432Node redirection | Explicitly use RegistryView.Registry64 for HKLM |
| Testing strategy | FakeRegistryProvider gives false confidence | Runtime verification via elevated launch + log check |
| UI/UX | UI freezing during tweaks | All operations async, progress indicators |
| State management | Windows Update reverts tweaks | Startup re-validation of all tweak states |
| Appx removal | (v2+) Breaking Windows functionality | Curated safe-package lists only |

## Sources

- Stack Overflow — Registry SetValue UnauthorizedAccessException: https://stackoverflow.com/questions/11768172/c-sharp-registry-setvalue-throws-unauthorizedaccessexception
- Microsoft Docs — RegistryKey.OpenSubKey: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.opensubkey
- WinAurex — Windows 11 Tweaks Guide: https://winaurex.vercel.app/tweaks/
- Kartones Blog — Disabling unneeded Windows 11 Services: https://blog.kartones.net/post/disabling-unneeded-windows-11-services/
- Chris Titus Tech — WinUtil: https://github.com/christitustech/winutil
- Microsoft Docs — Wow6432Node: https://learn.microsoft.com/en-us/windows/win32/win32apientry/understanding-the-registry-wrappers
- Microsoft Docs — RegistryView: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registryview
- Microsoft Docs — Fullscreen Optimizations registry: https://learn.microsoft.com/en-us/answers/questions/3741077/fullscreen-optimizations-windows-registry
- Reddit r/pcmasterrace — Windows 11 debloat: https://reddit.com/r/pcmasterrace/comments/1up1sc2/
