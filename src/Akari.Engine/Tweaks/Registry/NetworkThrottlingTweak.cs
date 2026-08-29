// REG-03: Network Throttling tweak.
// Hive: HKEY_LOCAL_MACHINE
// Key: SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile
// Value: NetworkThrottlingIndex, DWORD, enabled=0xFFFFFFFF (no throttling), disabled=0x00000003 (default)

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Registry;

/// <summary>
/// Network Throttling (REG-03): Sets NetworkThrottlingIndex to 0xFFFFFFFF to
/// disable network throttling for multimedia streams, reducing network latency.
/// </summary>
public class NetworkThrottlingTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "REG-03",
        Name = "Network Throttling Index",
        Category = "Registry",
        Type = TweakType.Registry,
        Description = "Disables network throttling (NetworkThrottlingIndex = 0xFFFFFFFF) to reduce latency for gaming traffic.",
        RegistryKey = @"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
        RegistryValueName = "NetworkThrottlingIndex",
        RegistryValueKind = RegistryValueKind.DWord,
        RegistryValueData = "4294967295",  // 0xFFFFFFFF (no throttling)
        RegistryRevertValueData = "3",       // 0x00000003 (default)
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 3,
    };
}
