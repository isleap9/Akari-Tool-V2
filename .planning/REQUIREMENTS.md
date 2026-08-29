# Requirements: Akari Tool V2

**Defined:** 2026-08-29
**Core Value:** One checklist, every gaming optimization: consolidate the scattered, hard-to-find Windows 11 gaming tweaks into a single modular UI where users toggle what they want and apply in one click.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Core Engine
- [ ] **ENG-01**: Registry provider abstraction with 2-arg OpenSubKey(path, true) writable overload (not 3-arg RegistryRights)
- [ ] **ENG-02**: ITweakEngine dispatch via Strategy pattern (Registry, Service, Process, Power, Network, File types)
- [ ] **ENG-03**: ITweakStateService tracks which tweaks are applied/reverted with JSON persistence
- [ ] **ENG-04**: ILogService writes to %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log
- [ ] **ENG-05**: All engine operations are async Task (never block UI thread)
- [ ] **ENG-06**: Startup re-validation of all tweak states against actual system (detect Windows Update reverts)

### Registry Tweaks
- [ ] **REG-01**: Game Mode toggle (registry-based)
- [ ] **REG-02**: Hardware-Accelerated GPU Scheduling (HAGS) enable/disable
- [ ] **REG-03**: NetworkThrottlingIndex set to 0xffffffff (remove network throttling)
- [ ] **REG-04**: Win32PrioritySeparation = 26 (CPU priority optimization)
- [ ] **REG-05**: Multimedia SystemProfile Tasks\Games GPU Priority = 8, Priority = 6, Scheduling Category = High
- [ ] **REG-06**: Visual effects optimization (disable animations, transparency, shadows)
- [ ] **REG-07**: Mouse acceleration disable (MouseSpeed = 0, SmoothMouse = 0)

### Service Management
- [ ] **SVC-01**: Disable Xbox background services (Xbox Live Auth Manager, Xbox Live Game Save, Gaming Services, GameDVR and Broadcast User Service)
- [ ] **SVC-02**: Disable GameDVR/Game Bar (registry-based and service-based)

### Process Management
- [ ] **PROC-01**: Set process priority for active games (High priority)
- [ ] **PROC-02**: Disable background processes during gaming (optional toggle)

### Power Management
- [ ] **PWR-01**: Activate Ultimate Performance power plan (powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61)
- [ ] **PWR-02**: Activate High Performance power plan as fallback

### Memory Management
- [ ] **MEM-01**: Toggle Windows memory compression off (Disable-MMAgent -MemoryCompression)

### User Interface
- [ ] **UI-01**: Modular checklist with categorized tweak groups (Registry, Services, Processes, Power, Memory, Network, Visual, Input)
- [ ] **UI-02**: Per-tweak toggle (individual apply/revert)
- [ ] **UI-03**: Require admin elevation on startup (requireAdministrator)
- [ ] **UI-04**: Display applied/not-applied state for each tweak
- [ ] **UI-05**: Apply all selected tweaks in one batch with progress indicator

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Monitoring & Performance
- **MON-01**: Real-time FPS counter overlay within the tool
- **MON-02**: CPU/memory usage graph
- **MON-03**: Boot time tracking and reporting

### Appx & Downloads
- **APPX-01**: Full uninstall of selected Windows Appx packages (Get-AppxPackage | Remove-AppxPackage)
- **DL-01**: Download page with curated links to gaming app installers (.exe/.msi)

### Advanced Features
- **ADV-01**: Automated system restore point creation before applying tweaks
- **ADV-02**: GPU-specific optimizations (NVIDIA/AMD registry settings)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Real-time performance overlay | Explicitly excluded — tool applies tweaks and exits, monitoring is v2+ |
| Automated restore point creation | User explicitly chose per-tweak revert only; user manages system restore externally |
| Microsoft Store / MSIX distribution | Personal tool — self-contained deployment only |
| Windows Insider build support | Targets latest stable Windows 11 builds only |
| Appx package management | Focus is on gaming performance tweaks; Appx removal is v2+ |
| Driver installation/updating | Risk of bricking; vendor-specific |
| Overclocking tools | Risk of hardware damage; requires vendor-specific APIs |
| Kernel-mode drivers | Requires driver signing, security review, huge complexity |
| Automated recommendation engine | Requires ML/data; user manually selects tweaks |
| Cross-platform support | Windows 11-specific by nature |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ENG-01 through ENG-03 | Phase 1 | Pending |
| ENG-04, ENG-05, ENG-06 | Phase 1 | Pending |
| REG-01 through REG-07 | Phase 1 | Pending |
| SVC-01, SVC-02 | Phase 1 | Pending |
| PROC-01, PROC-02 | Phase 1 | Pending |
| PWR-01, PWR-02 | Phase 2 | Pending |
| MEM-01 | Phase 2 | Pending |
| UI-01 through UI-02 | Phase 3 | Pending |
| UI-03, UI-04, UI-05 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 24 total
- Mapped to phases: 24
- Unmapped: 0 ✓

---

*Requirements defined: 2026-08-29*
*Last updated: 2026-08-29 after initial definition*
