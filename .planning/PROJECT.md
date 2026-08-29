# Akari Tool V2

## What This Is

A WinUI 3 MVVM toolbox for Windows 11 gamers that consolidates the best system-level gaming optimizations into a single modular checklist interface. Users browse categorized tweak groups (registry, services, processes, network QoS, memory, visual effects, power plans), toggle the ones they want, and apply them with admin elevation — no need to dig through Windows Settings, PowerShell scripts, or scattered guides.

## Core Value

One checklist, every gaming optimization: consolidate the scattered, hard-to-find Windows 11 gaming tweaks into a single modular UI where users toggle what they want and apply in one click.

## Business Context

<!-- OPTIONAL — only for monetized or customer-facing projects. Delete this section otherwise. -->

- **Customer**: Windows 11 gaming enthusiasts who want system-level optimizations without deep technical knowledge
- **Revenue model**: None — personal open-source hobby project
- **Success metric**: Number of categorized, effective gaming tweak groups accessible and applied through the UI
- **Strategy notes**: Complements AME Playbook — handles per-user system tweaks and gaming-specific optimizations not covered at the image level

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] **CORE-01**: Modular checklist UI with categorized tweak groups (registry, services, processes, network QoS, memory, visual effects, power plans)
- [ ] **CORE-02**: Admin elevation required for applying system-level changes
- [ ] **CORE-03**: Toggle individual categories on/off and apply selected tweaks
- [ ] **TWEAKS-01**: Windows 11 gaming registry optimizations (Game Mode, HAGS, visual effects, power plans)
- [ ] **TWEAKS-02**: Service management for gaming (stopping/staged background services)
- [ ] **TWEAKS-03**: Process priority and affinity optimization for active games
- [ ] **TWEAKS-04**: Network QoS for game traffic prioritization
- [ ] **TWEAKS-05**: Memory management tweaks (memory compression, large pages)
- [ ] **TWEAKS-06**: Visual effects reduction and input optimization (mouse acceleration, animations)

### Out of Scope

- [ ] Real-time monitoring/overlay — tool applies tweaks and exits, no FPS or performance graphs
- [ ] Automated restore point creation — user manages system restore points externally
- [ ] Microsoft Store distribution or MSIX packaging — personal tool only
- [ ] Windows Insider build support — targets latest stable Windows 11 builds only
- [ ] Appx package management — focused on system performance tweaks only

## Context

- WinUI 3 MVVM project using .NET 10 and MVVM Toolkit
- Self-contained deployment targeting win-x64
- Requires `requireAdministrator` in the application manifest
- Key .NET/registry lesson: `Win32RegistryProvider.SetValue/DeleteValue` must use 2-arg `OpenSubKey(path, true)` (writable) — the 3-arg overload with explicit `RegistryRights` doesn't cover all internal rights that `RegistryKey.SetValue()` needs, causing `UnauthorizedAccessException` even when elevated. `FakeRegistryProvider` (in-memory tests) does NOT enforce ACLs and cannot catch this. Must verify via actual elevated app launch + log check at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log`.

## Constraints

- **Tech stack**: C# .NET 10, WinUI 3, MVVM Toolkit — self-contained deployment (win-x64)
- **Elevation**: Admin rights required (`requireAdministrator`), Windows 11 stable builds only
- **Architecture**: MVVM pattern — `Akari.Core` (engine interfaces, tweak catalog), `Akari.Infrastructure` (executors, providers, state, logging), `Akari.App` (WinUI 3 Views/ViewModels)
- **Testing**: Unit tests must cover tweak engine dispatch logic with `FakeRegistryProvider` for in-memory testing — but runtime registry writes must be verified via elevated app launch + log check
- **Registry**: Must verify all registry writes via actual elevated app launch + log check — cannot rely on FakeRegistryProvider tests alone

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| WinUI 3 + MVVM Toolkit + .NET 10 | Modern Windows-native stack, familiar to the user, matches existing tooling context | — Pending |
| Moderate aggression level | User explicitly chose moderate (common gaming optimizations, not system-wide aggressive changes) | ✓ Good |
| Self-contained deployment (win-x64) | No framework dependency on user machine, simpler distribution for personal tool | — Pending |
| Modular checklist over one-click | User explicitly wants to toggle categories on/off | ✓ Good |
| No built-in monitoring | User said "just apply tweaks and exit" — monitoring is v2+ at best | ✓ Good |
| No automated restore points | Tool applies/reverts individual toggles; user manages system restore externally | ✓ Good |

---

*This document evolves at phase transitions and milestone boundaries.*

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

