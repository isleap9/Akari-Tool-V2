// MEM-01: Memory Compression toggle tweak.
// Disables Windows memory compression to reduce CPU overhead during gaming.
//
// From AkariOS Tweaks/3 Setup/2 Memory Compression.ps1:
//   Option 1 (Disable): Disable-MMAgent -MemoryCompression
//   Option 2 (Enable):  Enable-MMAgent -MemoryCompression
//   Option 3 (Check):   get-mmagent
//
// Memory compression can increase CPU overhead during high-memory-pressure
// gaming scenarios. Disabling it may help on systems with ample RAM (16GB+).
// Requires admin elevation and may require a system reboot to take effect.

using Akari.Engine.Core.Models;

namespace Akari.Engine.Tweaks.Memory;

/// <summary>
/// Memory Compression (MEM-01): Disables Windows memory compression to reduce
/// CPU overhead during gaming. Memory compression is a Windows 10/11 feature that
/// compresses memory pages in RAM instead of writing them to disk, but it can
/// increase CPU usage during high-pressure gaming scenarios.
/// </summary>
public class MemoryCompressionTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "MEM-01",
        Name = "Memory Compression",
        Category = "Memory",
        Type = TweakType.Memory,
        Description = "Disables Windows memory compression to reduce CPU overhead" +
                      " during high-memory-pressure gaming scenarios. Disabling" +
                      " memory compression may help on systems with ample RAM (16GB+)." +
                      " Requires admin elevation and may require a system reboot",
        PowerShellCommand = "Disable-MMAgent -MemoryCompression",
        PowerShellRevertCommand = "Enable-MMAgent -MemoryCompression",
        RequiresRestart = true,
        RequiresAdmin = true,
        SortOrder = 1,
    };
}