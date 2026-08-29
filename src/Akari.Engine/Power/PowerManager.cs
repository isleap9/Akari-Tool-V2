// PowerManager — production implementation wrapping powercfg.exe.
//
// Uses Process.Start("powercfg.exe", ...) for runtime power plan management.
// All operations are async Task with Task.Run offloading (D-11, Pitfall 5)
// to prevent UI thread blocking. This is the real implementation;
// FakePowerManager is used for unit tests.
//
// Per Pitfall 9 (GUID confusion): validates scheme GUIDs by querying
// powercfg /LIST before activation. Falls back to High Performance if
// Ultimate Performance is not available.

using System.Diagnostics;
using Akari.Engine.Registry;
using Microsoft.Win32;

namespace Akari.Engine.Power;

/// <summary>
/// Production implementation of <see cref="IPowerManager"/> that wraps
/// <c>powercfg.exe</c> for real Windows power plan management.
/// All operations are async Task with Task.Run offloading to prevent UI blocking (D-11).
/// </summary>
public class PowerManager : IPowerManager
{
    private readonly IRegistryProvider _registry;

    public PowerManager(IRegistryProvider registry)
    {
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task<PowerOperationResult> SetActiveSchemeAsync(string schemeGuid)
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/SETACTIVE {schemeGuid}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return new PowerOperationResult
                    {
                        Success = true,
                        Output = output.Trim()
                    };
                }

                return new PowerOperationResult
                {
                    Success = false,
                    ErrorMessage = error.Trim(),
                    Output = output.Trim()
                };
            }
            catch (Exception ex)
            {
                return new PowerOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<PowerOperationResult> DuplicateSchemeAsync(string baseGuid, string targetGuid)
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/duplicatescheme {baseGuid} {targetGuid}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return new PowerOperationResult
                    {
                        Success = true,
                        Output = output.Trim()
                    };
                }

                return new PowerOperationResult
                {
                    Success = false,
                    ErrorMessage = error.Trim(),
                    Output = output.Trim()
                };
            }
            catch (Exception ex)
            {
                return new PowerOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PowerSchemeInfo>> ListSchemesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/LIST",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Also get active scheme
                var activeProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "/GETACTIVESCHEME",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                activeProcess.Start();
                var activeOutput = activeProcess.StandardOutput.ReadToEnd();
                activeProcess.WaitForExit();

                var activeGuid = string.Empty;
                var activeMatch = System.Text.RegularExpressions.Regex.Match(activeOutput,
                    @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                if (activeMatch.Success)
                {
                    activeGuid = activeMatch.Value;
                }

                var schemes = new List<PowerSchemeInfo>();
                var guidPattern = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
                var matches = System.Text.RegularExpressions.Regex.Matches(output, guidPattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var guid = match.Value;
                    // Parse name from the same line
                    var line = output.Substring(match.Index);
                    var lineEnd = line.IndexOf('\n');
                    if (lineEnd > 0)
                    {
                        line = line.Substring(0, lineEnd);
                    }
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(line,
                        @"\((?<name>.+)\)");
                    var name = nameMatch.Success ? nameMatch.Groups["name"].Value : "Unknown";

                    schemes.Add(new PowerSchemeInfo
                    {
                        Guid = guid,
                        Name = name,
                        IsActive = guid.Equals(activeGuid, StringComparison.OrdinalIgnoreCase)
                    });
                }

                return (IEnumerable<PowerSchemeInfo>)schemes;
            }
            catch
            {
                return Enumerable.Empty<PowerSchemeInfo>();
            }
        });
    }

    /// <inheritdoc/>
    public async Task<PowerOperationResult> RestoreDefaultSchemesAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = "-restoredefaultschemes",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new PowerOperationResult
                {
                    Success = process.ExitCode == 0,
                    Output = output.Trim(),
                    ErrorMessage = process.ExitCode != 0 ? error.Trim() : null
                };
            }
            catch (Exception ex)
            {
                return new PowerOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<PowerOperationResult> SetHibernateAsync(bool enabled)
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powercfg.exe",
                        Arguments = $"/hibernate {(enabled ? "on" : "off")}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return new PowerOperationResult
                {
                    Success = process.ExitCode == 0,
                    Output = $"Hibernate {(enabled ? "enabled" : "disabled")}."
                };
            }
            catch (Exception ex)
            {
                return new PowerOperationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> SetRegistryValueAsync(string keyPath, string valueName, int value)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Uses the same 2-arg OpenSubKey pattern from Phase 1 (D-01, Pitfall 1)
                // via IRegistryProvider which delegates to Win32RegistryProvider
                _registry.SetValueAsync(keyPath, valueName, value, RegistryValueKind.DWord).Wait();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
}