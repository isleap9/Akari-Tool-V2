// Win32RegistryProvider — production registry provider using Microsoft.Win32.Registry.
//
// DESIGN CONSTRAINTS (per D-01, D-02, PITFALLS.md Pitfall 1 & 3):
//
// 1. MUST use the 2-arg OpenSubKey(path, true) writable overload.
//    NEVER use the 3-arg rights-based overload (OpenSubKey(path, rights)).
//    The 3-arg overload is insufficient — RegistryKey.SetValue()
//    internally needs QueryValues and ReadKey rights that the 3-arg security context doesn't grant,
//    causing UnauthorizedAccessException even when the process is elevated.
//
// 2. MUST use RegistryView.Registry64 explicitly for HKLM access.
//    Without this, 32-bit processes on 64-bit Windows are silently redirected to
//    HKLM\SOFTWARE\Wow6432Node, writing to the wrong location.
//
// 3. All operations are async Task with Task.Run offloading for blocking registry I/O.

using Microsoft.Win32;

namespace Akari.Engine.Registry;

/// <summary>
/// Production registry provider that performs real Windows registry operations.
/// Uses the 2-arg <c>OpenSubKey(path, true)</c> writable overload (never the 3-arg
/// rights-based overload) to prevent <see cref="UnauthorizedAccessException"/>
/// even when elevated (D-01, PITFALLS.md Pitfall 1).
/// Uses <see cref="RegistryView.Registry64"/> for HKLM access to avoid Wow6432Node
/// redirection on 64-bit Windows (D-02, PITFALLS.md Pitfall 3).
/// </summary>
public class Win32RegistryProvider : IRegistryProvider
{
    /// <summary>
    /// Parses a full registry key path into its hive and subkey components.
    /// Supports both short (HKLM:\, HKCU:\) and long (HKEY_LOCAL_MACHINE, HKEY_CURRENT_USER) prefixes.
    /// Returns a tuple of (RegistryHive, subKeyPath).
    /// </summary>
    internal static (RegistryHive hive, string subKey, RegistryView view) ParseKeyPath(string keyPath)
    {
        var trimmed = keyPath.Trim();

        // Try short-form and long-form prefixes, ordered by hive.
        // Short form uses colon (HKLM:\) or without (HKLM\), long form uses full name.
        if (trimmed.StartsWith("HKLM:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_LOCAL_MACHINE:", StringComparison.OrdinalIgnoreCase))
        {
            return (RegistryHive.LocalMachine, StripPrefix(trimmed, "HKLM"), RegistryView.Registry64);
        }

        if (trimmed.StartsWith("HKCU:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_CURRENT_USER:", StringComparison.OrdinalIgnoreCase))
        {
            return (RegistryHive.CurrentUser, StripPrefix(trimmed, "HKCU"), RegistryView.Registry64);
        }

        if (trimmed.StartsWith("HKCR:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_CLASSES_ROOT\\", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_CLASSES_ROOT:", StringComparison.OrdinalIgnoreCase))
        {
            return (RegistryHive.ClassesRoot, StripPrefix(trimmed, "HKCR"), RegistryView.Registry64);
        }

        if (trimmed.StartsWith("HKU:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_USERS\\", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_USERS:", StringComparison.OrdinalIgnoreCase))
        {
            return (RegistryHive.Users, StripPrefix(trimmed, "HKU"), RegistryView.Registry64);
        }

        if (trimmed.StartsWith("HKCC:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_CURRENT_CONFIG\\", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("HKEY_CURRENT_CONFIG:", StringComparison.OrdinalIgnoreCase))
        {
            return (RegistryHive.CurrentConfig, StripPrefix(trimmed, "HKCC"), RegistryView.Registry64);
        }

        throw new ArgumentException(
            $"Unrecognized registry hive prefix in key path: {keyPath}. " +
            "Expected prefix such as HKLM:\\, HKCU:\\, HKEY_LOCAL_MACHINE\\, or HKEY_CURRENT_USER\\.",
            nameof(keyPath));
    }

    /// <summary>
    /// Strips the hive prefix from the key path, returning only the subkey portion.
    /// Handles colon-separated (:), backslash-separated (\), and bare prefix forms.
    /// </summary>
    private static string StripPrefix(string keyPath, string shortPrefix)
    {
        // Handle "HKLM:\..." → "SOFTWARE\..."
        // Handle "HKLM\..." → "SOFTWARE\..."
        // Handle "HKLM:..." → "SOFTWARE\..."
        var span = keyPath.AsSpan();

        // Remove the short prefix (e.g. "HKLM")
        if (span.StartsWith(shortPrefix + ":", StringComparison.OrdinalIgnoreCase))
        {
            span = span[(shortPrefix.Length + 1)..];
            // Strip leading backslash if present
            if (span.StartsWith("\\", StringComparison.Ordinal)) span = span[1..];
        }
        else if (span.StartsWith(shortPrefix + "\\", StringComparison.OrdinalIgnoreCase))
        {
            span = span[(shortPrefix.Length + 1)..];
        }
        else if (span.StartsWith(shortPrefix, StringComparison.OrdinalIgnoreCase) &&
                 span.Length > shortPrefix.Length &&
                 (span[shortPrefix.Length] == ':' || span[shortPrefix.Length] == '\\'))
        {
            span = span[(shortPrefix.Length + 1)..];
            if (span.StartsWith("\\", StringComparison.Ordinal)) span = span[1..];
        }

        // Handle full hive names (HKEY_LOCAL_MACHINE, etc.)
        if (span.StartsWith("\\", StringComparison.Ordinal)) span = span[1..];

        return span.ToString();
    }

    /// <inheritdoc/>
    public async Task<T?> GetValueAsync<T>(string keyPath, string valueName)
    {
        return await Task.Run(() =>
        {
            var (hive, subKey, view) = ParseKeyPath(keyPath);
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, true); // 2-arg writable — D-01
            if (key == null) return default;

            var value = key.GetValue(valueName);
            if (value == null) return default;

            return (T?)(object?)value;
        });
    }

    /// <inheritdoc/>
    public async Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind)
    {
        await Task.Run(() =>
        {
            var (hive, subKey, view) = ParseKeyPath(keyPath);
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, true); // 2-arg writable — D-01
            if (key == null)
                throw new InvalidOperationException($"Registry key not found: {keyPath}");
            key.SetValue(valueName, value, kind);
        });
    }

    /// <inheritdoc/>
    public async Task<bool> KeyExistsAsync(string keyPath)
    {
        return await Task.Run(() =>
        {
            var (hive, subKey, view) = ParseKeyPath(keyPath);
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, true); // 2-arg writable — D-01
            return key != null;
        });
    }

    /// <inheritdoc/>
    public async Task DeleteValueAsync(string keyPath, string valueName)
    {
        await Task.Run(() =>
        {
            var (hive, subKey, view) = ParseKeyPath(keyPath);
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, true); // 2-arg writable — D-01
            if (key == null)
                throw new InvalidOperationException($"Registry key not found: {keyPath}");
            key.DeleteValue(valueName, false);
        });
    }
}
