# Research Summary: Akari Tool V2

**Domain:** Windows 11 gaming optimization toolbox (WinUI 3 MVVM)
**Researched:** 2026-08-29
**Overall confidence:** HIGH

## Executive Summary

Akari Tool V2 is a WinUI 3 MVVM toolbox for Windows 11 gamers that consolidates the best system-level gaming optimizations into a single modular checklist interface. The tool uses moderate-level tweaks (registry, services, processes, network QoS, memory, visual effects, power plans) — not aggressive system-wide changes. Users toggle individual tweak categories and apply them with admin elevation. The tool applies tweaks and exits — no real-time monitoring/overlay in v1.

The recommended stack is .NET 10 with Windows App SDK 1.7.x (stable), MVVM Toolkit 8.4.2, and self-contained win-x64 deployment. The architecture follows a Clean Layered pattern: Core (tweak definitions + interfaces), Infrastructure (concrete executors + Win32 providers), and Presentation (WinUI 3 Views/ViewModels). The most critical pitfall is the `UnauthorizedAccessException` from using the 3-arg `RegistryKey.OpenSubKey()` overload — the 2-arg writable overload must be used instead. FakeRegistryProvider tests cannot catch this (no ACL enforcement); runtime verification via elevated launch + log check is mandatory.

Key differentiators: modular checkbox UI (no one-click), JSON-backed tweak catalog (data-driven, no recompile), async engine (UI never blocks), and startup state reconciliation (detects tweaks reverted by Windows Updates). The tool fills a gap between "manually editing registry" and "full debloat suites like Chris Titus Tech's winutil" — it's focused specifically on gaming optimizations with moderate aggression and individual toggle control.

## Key Findings

**Stack:** .NET 10 + Windows App SDK 1.7.x stable + MVVM Toolkit 8.4.2, self-contained win-x64 deployment. Use `Microsoft.Extensions.DependencyInjection` for DI, `System.ServiceController` for services, `System.Diagnostics.Process` for process management, `powercfg.exe` for power plans. Registry access via 2-arg `OpenSubKey(path, true)` with explicit `RegistryView.Registry64` for HKLM.

**Table Stakes:** Game Mode toggle, HAGS, Ultimate Performance power plan, background app/service disabling (Xbox services, GameDVR), visual effects optimization, memory compression toggle, NetworkThrottlingIndex, CPU/GPU priority for games, mouse acceleration disable. These are the baseline expectations per YouTube/Reddit guides (Lecctron, The Software Guy).

**Watch Out For:** (1) `UnauthorizedAccessException` on registry writes — use 2-arg `OpenSubKey(path, true)`, NOT 3-arg with `RegistryRights.SetValue`; (2) Wow6432Node redirection — always use `RegistryView.Registry64` for HKLM; (3) FakeRegistryProvider tests give false confidence — must verify via elevated runtime + log check; (4) Windows Updates revert tweaks — implement startup state re-validation; (5) UI freezing — all operations must be async.

## Implications for Roadmap

Based on research and the Coarse granularity setting, suggested phase structure:

1. **Foundation & Engine Core** — Registry provider abstraction (with the 2-arg OpenSubKey workaround baked in), tweak catalog data structure, engine dispatch (Strategy pattern), state service, logging. This phase MUST address Pitfalls #1, #2, #3 from day one — no room for FakeRegistryProvider false confidence.
   - Addresses: Registry engine, ITweakEngine, ITweakExecutor, IRegistryProvider, Win32RegistryProvider, FakeRegistryProvider, ITweakStateService, ILogService
   - Avoids: UnauthorizedAccessException, Wow6432Node issues, test false confidence

2. **Service & Process Management** — ServiceOperationExecutor (Xbox services, GameDVR, background services), ProcessOperationExecutor (process priority, background process management). Depends on the engine from Phase 1.
   - Addresses: ServiceOperationExecutor, ProcessOperationExecutor, power plan via powercfg
   - Avoids: Service dependency chain issues, UI freezing

3. **Network, Memory & Visual Tweaks + UI** — NetworkOperationExecutor (QoS, NetworkThrottlingIndex), memory compression toggles, visual effects, mouse acceleration. Plus the WinUI 3 modular checklist UI wired to the engine.
   - Addresses: NetworkOperationExecutor, modular checklist UI, power plan UI, state reconciliation
   - Avoids: UI freezing, Windows Update state drift

4. **Integration & Verified Release** — Full integration testing, elevated runtime verification, log validation, end-to-end tweak application/revert flow, build self-contained deployment package.
   - Addresses: Runtime verification, self-contained deployment, complete test suite
   - Avoids: Runtime-only crashes, deployment issues

**Phase ordering rationale:** Registry engine is the dependency for 80% of tweaks — must be built and verified first (including the ACL workaround). Service/process management comes next (depends on engine, covers Xbox/GameDVR services). Network/memory/visual tweaks + UI is the user-facing layer. Integration/testing is last (requires everything working end-to-end with elevated verification).

**Research flags for phases:**
- Phase 1: Standard patterns — registry/provider patterns are well-documented. Skip per-phase research.
- Phase 2: Standard patterns — ServiceController and Process APIs are well-documented. Skip per-phase research.
- Phase 3: Standard patterns — PowerCfg, QoS APIs well-documented. Skip per-phase research.
- Phase 4: Standard patterns — integration testing for self-contained deployment is routine. Skip per-phase research.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All technologies are current (2026), well-documented, compatible |
| Features | HIGH | Extensive community documentation (YouTube, Reddit, blog guides) with specific registry paths |
| Architecture | HIGH | Clean layered pattern is standard for WinUI 3 + MVVM; Microsoft docs endorse it |
| Pitfalls | HIGH | Specific pitfall (3-arg vs 2-arg OpenSubKey) confirmed via Stack Overflow and Microsoft Docs; user memory confirms this exact issue |

## Gaps to Address

- GPU-specific optimizations (NVIDIA Control Panel, AMD Radeon Settings registry keys) — these are vendor-specific and may need phase-specific research during planning
- Appx removal (v2+ feature, explicitly out of scope for v1) — flagged but not researched in detail
- Real-time performance monitoring (v2+ feature, explicitly out of scope for v1)
