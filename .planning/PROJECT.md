# Akari Tool V2

## What This Is

A WinUI 3 MVVM (.NET/C#) Windows 11 gaming optimization tool that consolidates every system tweak, Appx removal, real-time monitoring, and curated game-related downloads into a single modular checklist interface — so users never need to open Windows Settings or manually hunt through obscure menus. Built for users who want fine-grained, per-tweak control over aggressive system-level changes with admin elevation.

## Core Value

One place to control everything: every Windows 11 gaming-related optimization (registry tweaks, service management, process control, network QoS, memory/disk/audio settings, Appx removal, and performance monitoring) accessible through a single modular checklist — eliminating the need to touch Windows Settings, PowerShell, or third-party config tools.

## Business Context

<!-- OPTIONAL — only for monetized or customer-facing projects. Delete this section otherwise. -->

- **Customer**: The user themselves — personal/gaming enthusiast power user
- **Revenue model**: None — personal open-source tool
- **Success metric**: Number of system-level optimizations accessible from the UI, verified as effective on latest stable Windows 11 builds
- **Strategy notes**: Complements AME Playbook — handles per-user system tweaks and gaming-specific optimizations that AME Playbook doesn't cover at the image level

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] **CORE-01**: Modular checklist UI showing all gaming optimization tweaks (registry, services, processes, network, memory, disk, audio, visual effects, input)
- [ ] **CORE-02**: Per-tweak aggression level selection (conservative/recommended/aggressive) with admin elevation
- [ ] **CORE-03**: Real-time system monitoring scorecard (FPS, CPU/memory usage, boot time)
- [ ] **CORE-04**: Per-tweak apply/revert with individual toggle control
- [ ] **TWEAKS-01**: Network QoS prioritization for game traffic (multicast prioritization, outbound bandwidth allocation)
- [ ] **TWEAKS-02**: Memory management tweaks (memory compression, large page allocation, standby list trimming)
- [ ] **TWEAKS-03**: Disk I/O optimization (disable NTFS last access timestamps, defrag behavior tuning)
- [ ] **TWEAKS-04**: Audio latency reduction (disable audio processing enhancements)
- [ ] **TWEAKS-05**: Visual effects and input tweaks (disable mouse acceleration, filter keys, animation reduction)
- [ ] **APPX-01**: Full uninstall of selected Windows Appx packages (Get-AppxPackage | Remove-AppxPackage)
- [ ] **DL-01**: Download page with curated links to .exe/.msi installers for selected gaming apps

### Out of Scope

- [ ] Real-time FPS monitoring overlay — too complex for v1, PerformanceCounter polling is sufficient
- [ ] Automated optimization recommendations — user manually selects tweaks
- [ ] System Restore point automation — user manages restore points externally
- [ ] Microsoft Store distribution or MSIX packaging — personal tool only
- [ ] Windows Insider build support — targets latest stable Windows 11 builds only

## Context

- Based on reverse-engineering the Vain Toolbox binary (91MB .NET 8 self-contained WinUI 3 app) and AME Playbook complement positioning
- Existing codebase has partial Phase 1 work (SystemTweaks, PrivacyTweaks, PowerTweaks) but not wired into UI
- Registry writes require `requireAdministrator` and use self-contained deployment (SelfContained=true, RuntimeIdentifiers=win-x64)
- Key .NET/registry lesson: `Win32RegistryProvider.SetValue/DeleteValue` must use 2-arg `OpenSubKey(path, true)` (writable) — the 3-arg overload with explicit `RegistryRights` doesn't cover all internal rights that `RegistryKey.SetValue()` needs, causing `UnauthorizedAccessException` even when elevated. `FakeRegistryProvider` (in-memory tests) does NOT enforce ACLs and cannot catch this.
- Log file at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` is the ground-truth feedback loop for runtime-only failures

## Constraints

- **Tech stack**: C# .NET 10, WinUI 3, MVVM Toolkit — must build as self-contained deployment (win-x64)
- **Requirements**: Admin elevation (requireAdministrator), Windows 11 stable builds only
- **Architecture**: Modular MVVM — `Akari.Core` (engines/interfaces/tweak catalog), `Akari.Infrastructure` (executors/providers/state/logging), `Akari.App` (WinUI 3 Views/ViewModels)
- **Testing**: Unit tests must cover the tweak engine dispatch logic (FakeRegistryProvider for in-memory testing)
- **Registry**: Must verify all registry writes via actual elevated app launch + log check — cannot rely on FakeRegistryProvider tests alone

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| WinUI 3 + MVVM Toolkit + .NET 10 | Familiar, modern Windows-native stack with good community support, matches Vain Toolbox heritage | — Pending |
| Self-contained deployment (win-x64) | No framework dependency on user machine, simpler distribution for personal tool | — Pending |
| Modular checklist over one-click | User explicitly wants per-tweak control with aggression levels | ✓ Good |
| Appx full uninstall | User wants complete removal, user manages system restore points | ✓ Good |
| PerformanceCounter for monitoring | User said "just to monitor their machine" — ETW is overkill for v1 | ✓ Good |
| No automated rollback | Explicit user decision — user manages system restore points | ✓ Good |

---

*This document evolves at phase transitions and milestone boundaries.*
