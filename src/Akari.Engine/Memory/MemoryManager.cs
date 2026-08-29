// MemoryManager — production implementation wrapping PowerShell MMAgent cmdlets.
//
// Uses PowerShell invocation of Disable-MMAgent / Enable-MMAgent -MemoryCompression
// for runtime memory management. All operations are async Task with Task.Run
// offloading (D-11, Pitfall 5) to prevent UI thread blocking.
// This is the real implementation; FakeMemoryManager is used for unit tests.
//
// From AkariOS Tweaks/3 Setup/2 Memory Compression.ps1:
//   Disable: Disable-MMAgent -MemoryCompression
//   Enable:  Enable-MMAgent -MemoryCompression
//   Check:   Get-MMAgent

using System.Diagnostics;

namespace Akari.Engine.Memory;

/// <summary>
/// Production implementation of <see cref="IMemoryManager"/> that wraps PowerShell
/// MMAgent cmdlets for Windows memory compression management.
/// All operations are async Task with Task.Run offloading to prevent UI blocking (D-11).
/// </summary>
public class MemoryManager : IMemoryManager
{
    /// <inheritdoc/>
    public async Task<MemoryOperationResult> DisableCompressionAsync()
    {
        return await Task.Run(async () =>
        {
            var command = "Disable-MMAgent -MemoryCompression";
            return await ExecutePowerShellAsync(command);
        });
    }

    /// <inheritdoc/>
    public async Task<MemoryOperationResult> EnableCompressionAsync()
    {
        return await Task.Run(async () =>
        {
            var command = "Enable-MMAgent -MemoryCompression";
            return await ExecutePowerShellAsync(command);
        });
    }

    /// <inheritdoc/>
    public async Task<bool> IsCompressionEnabledAsync()
    {
        return await Task.Run(async () =>
        {
            var result = await ExecutePowerShellAsync("Get-MMAgent");
            // Parse output for MemoryCompressionEnabled property
            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                return result.Output.Contains("True", StringComparison.OrdinalIgnoreCase)
                    && result.Output.Contains("MemoryCompressionEnabled");
            }
            return false;
        });
    }

    /// <summary>
    /// Executes a PowerShell command and captures the output.
    /// </summary>
    private static async Task<MemoryOperationResult> ExecutePowerShellAsync(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return new MemoryOperationResult
                {
                    Success = true,
                    Output = output.Trim()
                };
            }

            return new MemoryOperationResult
            {
                Success = false,
                ErrorMessage = error.Trim(),
                Output = output.Trim()
            };
        }
        catch (Exception ex)
        {
            return new MemoryOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}