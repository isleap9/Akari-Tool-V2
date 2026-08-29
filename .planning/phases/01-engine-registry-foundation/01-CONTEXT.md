# Phase 1: Engine & Registry Foundation - Context

**Gathered:** 2026-08-29
**Status:** Ready for planning

<domain>
## Phase Boundary

Deliver a working backend engine that can apply and revert 7 registry-based gaming tweaks end-to-end. The engine abstracts registry access behind a provider interface, dispatches tweak operations through a strategy pattern, persists tweak state to JSON with startup re-validation, and logs all operations to `%LOCALAPPDATA%\Akari\App\logs\`. No UI in this phase — pure backend with unit tests.
</domain>

<decisions>
## Implementation Decisions

### Registry Access
- **D-01:** Use a 2-arg `OpenSubKey(path, true)` writable overload instead of the 3-arg `RegistryRights`-based overload in `Win32RegistryProvider`. The 3-arg overload does not cover all internal rights that `RegistryKey.SetValue()` requires, causing `UnauthorizedAccessException` even when elevated — **Reversibility:** one-way — RegistryKey's internal security check requires additional rights beyond SetValue that the 3-arg overload's security context doesn't grant; cannot be fixed without changing every caller
- **D-02:** Explicitly specify `RegistryView.Registry64` when opening `HKLM` keys. Without this, 32-bit processes on 64-bit Windows get redirected to `Wow6432Node`, silently writing to the wrong registry location — **Reversibility:** costly — every HKLM write call would need to be audited and fixed; existing incorrect values must be cleaned up

[auto] [Gray Area: Registry Provider Strategy] — Q: "Which registry provider abstraction pattern?" → Selected: "IRegistryProvider interface with Win32RegistryProvider and FakeRegistryProvider (2-arg OpenSubKey, writable)" (recommended default)
[auto] [Gray Area: Tweak Catalog Discovery] — Q: "How should tweaks be cataloged?" → Selected: "JSON-driven catalog with typed definitions, Strategy pattern for execution" (recommended default)
[auto] [Gray Area: State Persistence] — Q: "How to persist tweak state?" → Selected: "JSON file in %LOCALAPPDATA%\Akari\App\state.json with startup re-validation" (recommended default)
[auto] [Gray Area: Async Operations] — Q: "How to handle async operations?" → Selected: "async Task with Task.Run offloading, async/await throughout to prevent UI freezing" (recommended default)

### Logging Strategy
- **D-03:** Log file at `%LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log` — this is the ground-truth feedback loop for runtime-only failures that `FakeRegistryProvider` tests cannot catch. — **Reversibility:** reversible — only file path changes; low cost
- **D-04:** FakeRegistryProvider tests are logic-only validation — they cannot enforce ACLs and will NOT surface `UnauthorizedAccessException`. Runtime verification via elevated launch + log file check is mandatory for every tweak category. — **Reversibility:** one-way — cannot be retroactively added to a non-logged engine design

### Startup Behavior
- **D-05:** The state service must perform startup re-validation — on tool launch, read the persisted state file and verify each "applied" tweak's registry value still matches expected. Windows Updates can silently revert registry tweaks, so stale state must be detected and surfaced as "not applied". — **Reversibility:** costly — state schema and re-validation logic would need redesign if added later

</decisions>

<canonical_refs>
## Canonical References

### Core Project Files
- `.planning/ROADMAP.md` §Phase 1 — Phase 1 goal, plans, and success criteria
- `.planning/REQUIREMENTS.md` §ENG-01 through ENG-06, REG-01 through REG-07 — Locked requirements for this phase
- `.planning/STATE.md` §Accumulated Context — Decisions that carry forward

### Research & Pitfalls
- `.planning/research/PITFALLS.md` — Critical pitfalls for registry writes (2-arg OpenSubKey, Wow6432Node, FakeRegistryProvider limitations, async, startup re-validation)
- `.planning/research/ARCHITECTURE.md` §Recommended Architecture — Clean layered architecture, engine dispatch, state service
- `.planning/research/STACK.md` — .NET 10, WinUI 3, Windows App SDK, Registry APIs
- `.planning/research/SUMMARY.md` — Research synthesis and key takeaways

### Implementation References for Downstream Agents
- `.hermes/HERMES.md` — Project instruction file (runtime: claude, stack: .NET 10, conventions)

**No external specs — requirements fully captured in decisions above.**
</canonical_refs>

<specifics>
## Specific Ideas

- **Pitfall prevention is architectural**: The 2-arg `OpenSubKey(path, true)` and `RegistryView.Registry64` patterns must be baked into the `Win32RegistryProvider` from day one — the roadmapper explicitly scoped this as non-negotiable because `FakeRegistryProvider` tests cannot catch runtime ACL failures.
- **The engine is the foundation**: Phase 2 (services) and Phase 3 (power/memory) depend on the verified working engine from Phase 1. This is the critical path.
- **JSON-driven tweak catalog**: Each tweak is a typed definition (name, registry path, value name, expected value, revert value) loaded from a JSON catalog, allowing new tweaks to be added without code changes.
- **Async Task pattern**: All engine operations must be `async Task` — non-blocking registry operations with `Task.Run` offloading for the actual system calls.
</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

### Reviewed Todos (not folded)

None.
</deferred>

---

*Phase: 1-Engine & Registry Foundation*
*Context gathered: 2026-08-29*
