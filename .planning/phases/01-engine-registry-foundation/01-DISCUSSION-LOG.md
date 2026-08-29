# Phase 1 Discussion Log

**Phase:** 1 — Engine & Registry Foundation
**Date:** 2026-08-29
**Mode:** --auto (recommended options selected)
**Status:** Discussion complete

---

## Gray Areas & Auto-Selected Decisions

| # | Gray Area | Question | Recommendation | Selection |
|---|-----------|----------|----------------|-----------|
| 1 | Registry Provider Strategy | Which registry provider abstraction pattern? | IRegistryProvider with Win32RegistryProvider + FakeRegistryProvider | Selected |
| 2 | Tweak Catalog Discovery | How should tweaks be cataloged and discovered? | JSON-driven catalog with typed definitions, Strategy pattern | Selected |
| 3 | State Persistence | How to persist tweak application state? | JSON file in %LOCALAPPDATA%\Akari\App\state.json with re-validation | Selected |
| 4 | Async Operations | How to handle async operations for system calls? | async Task with Task.Run offloading, async/await throughout | Selected |

[auto] [Area: Registry Provider Strategy] — Q: "Which registry provider abstraction pattern?" → Selected: "IRegistryProvider interface with Win32RegistryProvider and FakeRegistryProvider" (recommended default). Rationale: separates production ACL-enforced writes from in-memory test logic; 2-arg OpenSubKey prevents UnauthorizedAccessException.

[auto] [Area: Tweak Catalog Discovery] — Q: "How should tweaks be cataloged?" → Selected: "JSON-driven catalog with typed definitions" (recommended default). Rationale: allows adding new tweaks without code changes; aligns with modular checklist UI in Phase 4.

[auto] [Area: State Persistence] — Q: "How to persist tweak state?" → Selected: "JSON file with startup re-validation" (recommended default). Rationale: required by ENG-06 and Pitfall #7; Windows Updates can revert registry values silently.

[auto] [Area: Async Operations] — Q: "How to handle async operations?" → Selected: "async Task with Task.Run offloading" (recommended default). Rationale: required by ENG-05; prevents UI freezing during system operations.

## Prior Decisions Leveraged

From PROJECT.md / REQUIREMENTS.md:
- Admin elevation required for all system changes
- Registry-based tweaks only in Phase 1 (services, processes, power, memory in later phases)
- 7 registry tweaks: Game Mode, HAGS, NetworkThrottlingIndex, CPU priority, multimedia, visual effects, mouse acceleration

## Claude's Discretion

- All gray areas used recommended defaults (auto mode).
- No SPEC.md exists — decisions are captured in 01-CONTEXT.md, not locked by a spec.

## Deferred Ideas

None — discussion stayed within phase scope.
