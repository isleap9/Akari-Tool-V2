# Phase 01: Engine & Registry Foundation — Execution Plan

**Phase:** 01-engine-registry-foundation
**Goal:** Deliver a working backend engine that can apply and revert 7 registry-based gaming tweaks end-to-end, with state tracking, logging, and startup re-validation to detect Windows Update reverts.
**Granularity:** Coarse (3 plans)
**Model Profile:** Adaptive

---

## User Story (MVP)

**As a** Windows 11 gamer using Akari Tool V2
**I want to** toggle Gaming Mode and HAGS with a single click and see their state tracked
**so that** I know my optimizations are active and get warned if Windows Update reverted them.

---

## Plan Overview

| Plan | Name | Wave | Dependencies | Requirements | Est. Tokens | Type |
|------|------|------|-------------|-------------|-------------|------|
| 01-01 | Registry Provider Abstraction | 1 | — | ENG-01 | 18K | Tracer + 2 auto |
| 01-02 | Tweak Engine Core | 2 | 01-01 | ENG-02, ENG-03, ENG-05, ENG-06 | 32K | Tracer + 2 auto |
| 01-03 | 7 Registry Tweaks + Batch | 3 | 01-02 | REG-01–07 | 48K | Tracer + 2 auto |

**Total:** 3 plans, 9 tasks, ~98K tokens estimated

---

## Wave Execution

- **Wave 1:** 01-01 — Registry provider (foundation, parallel-safe, no deps)
- **Wave 2:** 01-02 — Engine core + state + logging (depends on provider)
- **Wave 3:** 01-03 — All 7 tweaks + batch apply (depends on engine)

---

## Dependency Graph

```
01-01 (Registry Provider)
    ↓
01-02 (Engine Core + State + Logging)
    ↓
01-03 (7 Registry Tweaks + Batch Apply)
```

All plans use tracer-first decomposition: each plan leads with one end-to-end tracer task, followed by expansion tasks.

---

## Key Decisions (from CONTEXT.md)

| Decision | ID | Impact |
|----------|----|--------|
| 2-arg OpenSubKey(path, true) — NOT 3-arg RegistryRights | D-01 | Win32RegistryProvider must never use RegistryRights overload — causes UnauthorizedAccessException |
| RegistryView.Registry64 for HKLM | D-01 | Prevents Wow6432Node silent redirection on 32-bit processes |
| Log file at %LOCALAPPDATA%\Akari\App\logs\ | D-03 | Ground-truth for runtime ACL failures FakeRegistryProvider can't catch |
| FakeRegistryProvider is logic-only | D-04 | Unit tests validate logic; Elevated launch + log check required for ACL verification |
| State service startup re-validation | D-05 | Detects Windows Update reverts — ENG-06 |

---

## Success Criteria (from ROADMAP.md)

1. ✅ Launch as admin, log file contains no `UnauthorizedAccessException`
2. ✅ Toggle Game Mode (REG-01) on/off — registry value correctly set and verified at runtime
3. ✅ Restart after Windows Update, detects reverted tweaks via startup re-validation
4. ✅ Apply all 7 tweaks in batch — async without UI freezing
5. ✅ HAGS (REG-02) writes to 64-bit HKLM (not Wow6432Node)

---

## Verification Strategy

**Unit tests (FakeRegistryProvider):** Validate logic for all engine + tweak operations
- `dotnet test src/Akari.Engine.Tests/` — full suite
- Covers: provider abstraction, engine dispatch, state persistence, re-validation, logging, all 7 tweak definitions

**Runtime verification (elevated launch + log check):** The ONLY way to catch UnauthorizedAccessException and verify 64-bit HKLM writes
- Requires admin elevation — `requireAdministrator` manifest (Phase 4) or manual Run As Admin
- Check `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` for:
  - No `UnauthorizedAccessException` entries (Pitfall 1 — 3-arg OpenSubKey)
  - No `Wow6432Node` in registry write logs (Pitfall 3 — RegistryView.Registry64)
- Verify via `regedit` that HAGS value is at `HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode` (not Wow6432Node)

---

## Files Produced

| File | Purpose |
|------|---------|
| `01-SKELETON.md` | Walking skeleton (architectural decisions + one-slice proof) |
| `01-01-PLAN.md` | Registry provider abstraction |
| `01-02-PLAN.md` | Tweak engine core + state + logging |
| `01-03-PLAN.md` | 7 registry tweaks + batch apply |
| `01-PLAN.md` | This orchestration summary |

---

## Requirement Coverage

| Requirement | Covered In Plan |
|-------------|----------------|
| ENG-01: Registry provider abstraction | 01-01 |
| ENG-02: Tweak engine Strategy dispatch | 01-02 |
| ENG-03: State service (JSON + re-validation) | 01-02 |
| ENG-04: Async operations (no UI blocking) | 01-01, 01-02, 01-03 |
| ENG-05: File logging to %LOCALAPPDATA% | 01-02 |
| ENG-06: Startup re-validation | 01-02 |
| REG-01: Game Mode | 01-03 |
| REG-02: HAGS | 01-03 |
| REG-03: NetworkThrottlingIndex | 01-03 |
| REG-04: Win32PrioritySeparation | 01-03 |
| REG-05: Multimedia Tasks | 01-03 |
| REG-06: Visual Effects | 01-03 |
| REG-07: Mouse Acceleration | 01-03 |

**Coverage: 13/13 requirements mapped ✓ | 0 unmapped**
