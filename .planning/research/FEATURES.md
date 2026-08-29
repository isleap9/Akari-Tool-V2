# Feature Landscape: Akari Tool V2

**Domain:** Windows 11 gaming optimization toolbox
**Researched:** 2026-08-29

## Table Stakes

Features users expect from a Windows 11 gaming optimization tool. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|-------------|------------|-------|
| Game Mode toggle | Windows 10+ builds in Game Mode; users expect to enable/disable it | Low | Registry: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GameList or Settings > Gaming |
| Hardware-Accelerated GPU Scheduling (HAGS) | Widely recommended for lower CPU overhead in games | Low | Registry: HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\HwSchMode (DWORD=2) |
| Ultimate Performance power plan | Eliminates CPU throttling during gaming | Low | powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 |
| Disable background apps | Reduces CPU/memory competition during games | Low | Settings > Apps > Startup or registry-based |
| Visual effects optimization | Reduces GPU/CPU overhead from animations, transparency | Low | SystemPropertiesPerformance or registry |
| Memory compression toggle | Reduces memory overhead, can improve gaming stutters | Medium | Disable-MMAgent -MemoryCompression (PowerShell) or registry HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management |
| Network throttling index | Improves game network responsiveness (lower ping/packet loss) | Low | HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\NetworkThrottlingIndex (DWORD=0xffffffff) |
| CPU priority/GPU priority for games | Directs more CPU/GPU resources to active games | Low | HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games |
| Disable GameDVR/Background Apps | GameDVR consumes resources in background | Medium | HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR or services.msc |
| Disable Xbox services | Xbox services consume background resources | Low | Services: Xbox Live Auth Manager, Xbox Live Game Save, Gaming Services, GameDVR and Broadcast User Service |
| Mouse acceleration disable | Competitive gamers need raw mouse input | Low | Registry-based (MouseSpeed, MouseSensitivity) |
| Standby list cleaner | Clears standby memory to prevent game stutters | Medium | Requires working set trimming or RAMMap automation |

## Differentiators

Features that set the product apart — not expected but valued by enthusiasts.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Modular checklist UI | Users toggle individual tweaks, not monolithic "optimize all" | High | UI/UX design — clear categories, toggle states, descriptions |
| Per-tweak rollback state | Users see what's applied and can revert individually | Medium | Track applied state in JSON state file |
| Tweak categorization | Registry, services, network, memory, GPU, audio, input — grouped for clarity | Medium | Organization and labeling |
| Power plan management | Switch between Balanced, High Performance, Ultimate Performance | Medium | Requires elevation, GUID-based plan management |
| Network QoS policy management | Dedicated QoS rules for game traffic | High | Set-NetQosPolicy PowerShell or WMI ROOT\StandardCimv2 |
| Audio service optimization | Disable audio enhancements for lower latency | Low | WASAPI exclusive mode, audio service tweaks |
| Full-screen optimizations | Per-game FSE toggle, GameDVR exclusions | Medium | Requires per-process configuration |

## Anti-Features

Features to deliberately NOT build.

| Anti-Feature | Why Avoid | What to Do Instead |
|-------------|-----------|-------------------|
| Overclocking tools | Risk of hardware damage, requires vendor-specific APIs | Leave to MSI Afterburner, Intel XTU, AMD Ryzen Master |
| Driver installation/updating | Risk of bricking, vendor-specific, requires reboot | Link to manufacturer sites or leave to user |
| Bloatware removal (beyond Appx) | Risk of breaking Windows, massive scope creep | Focus only on gaming-specific optimizations |
| Real-time performance overlay | Complex ETW integration, performance overhead | Keep as external tool (RTSS, MSI Afterburner) |
| Automated recommendation engine | Requires ML/data, unreliable on diverse hardware | User manually selects which tweaks to apply |
| BIOS-level optimization | Cannot be done from OS, requires reboot to BIOS | Document BIOS settings, let user do manually |
| Kernel-mode drivers | Huge complexity, signing requirements, security review | Use only documented Windows APIs (registry, services, PowerCfg) |
| Telemetry disabling | Broad-scope, may break Windows Update | Only gaming-specific telemetry, leave system telemetry alone |

## Feature Dependencies

```
Game Mode toggle → Registry engine
HAGS → Registry engine
Ultimate Performance → PowerCfg engine
Memory compression → PowerShell engine
Network throttling → Registry engine
CPU/GPU priority → Registry engine
Mouse acceleration → Registry engine
Disable GameDVR → Registry + Service engine
Disable Xbox services → Service engine
Background apps → Registry + Service engine
Standby list cleaner → Process engine
Visual effects → Registry engine
```

## MVP Recommendation

Prioritize:
1. Registry engine (foundation for most tweaks)
2. Service management (Xbox services, GameDVR)
3. Power plan management (Ultimate Performance)
4. Network QoS (low hanging: NetworkThrottlingIndex)
5. Modular checklist UI (to expose the above)

Defer:
- Audio service optimization (simple but lower impact)
- Standby list cleaner (requires more complex process working set management)
- Per-tweak rollback state tracking (can start with simple apply/revert)

## Sources

- WinAurex Windows 11 Tweaks Guide: https://winaurex.vercel.app/tweaks/
- Kartones Blog — Optimizing Windows 11 for Gaming: https://blog.kartones.net/post/optimizing-windows-11-for-gaming/
- Kartones Blog — Disabling unneeded Windows 11 Services: https://blog.kartones.net/post/disabling-unneeded-windows-11-services/
- Reddit r/pcmasterrace — Windows 11 debloat guide: https://reddit.com/r/pcmasterrace/comments/1up1sc2/
- YouTube — "Boost FPS & Fix Lag – 10 Deep Windows Tweaks for Gaming" (The Software Guy)
- YouTube — "How to Fully Optimize Windows 11 For Gaming" (Lecctron)
- Microsoft Docs — MMCSS: https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
- Microsoft Docs — QoS Policy: https://learn.microsoft.com/en-us/windows-server/networking/technologies/qos/qos-policy-top
- Microsoft Docs — RegistryKey.OpenSubKey: https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.registrykey.opensubkey
- Microsoft Learn — GameDVR registry settings: https://learn.microsoft.com/en-us/answers/questions/3741077/fullscreen-optimizations-windows-registry
