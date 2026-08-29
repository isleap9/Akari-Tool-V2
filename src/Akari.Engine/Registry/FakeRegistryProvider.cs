// FakeRegistryProvider — in-memory registry provider for unit tests.
//
// CRITICAL (D-04, PITFALLS.md Pitfall 2):
// This provider is an in-memory ConcurrentDictionary with NO ACL enforcement.
// It CANNOT catch UnauthorizedAccessException or other runtime security failures
// that the real Win32RegistryProvider would encounter. Unit tests using this
// provider validate LOGIC ONLY. Runtime verification of actual registry writes
// must be performed via an elevated app launch and log file inspection at
// %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.

using System.Collections.Concurrent;
using Microsoft.Win32;

namespace Akari.Engine.Registry;

/// <summary>
/// In-memory implementation of <see cref="IRegistryProvider"/> for unit tests.
/// Does NOT enforce ACLs — logic-only validation (per D-04, PITFALLS.md Pitfall 2).
/// Runtime ACL verification requires an actual elevated launch + log check.</summary>
public class FakeRegistryProvider : IRegistryProvider
{
    /// <summary>
    /// Composite key: "{keyPath}\\{valueName}" — stores the raw value object.
    /// Key-only entries (no values) are tracked with valueName "__key__".
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _store = new();

    /// <summary>
    /// Marker used in <see cref="_store"/> to indicate a key exists without any values.
    /// </summary>
    private const string KeyMarker = "__key_exists__";

    /// <summary>
    /// Computes the composite dictionary key for a key path + value name pair.
    /// </summary>
    private static string Compose(string keyPath, string valueName) =>
        $"{Normalize(keyPath)}\\{valueName}";

    private static string Normalize(string keyPath) =>
        keyPath.Trim().TrimEnd('\\').ToUpperInvariant();

    /// <inheritdoc/>
    public async Task<T?> GetValueAsync<T>(string keyPath, string valueName)
    {
        await Task.CompletedTask; // async signature consistency

        var compositeKey = Compose(keyPath, valueName);
        if (_store.TryGetValue(compositeKey, out var value))
        {
            return (T?)value;
        }
        return default;
    }

    /// <inheritdoc/>
    public async Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind)
    {
        await Task.CompletedTask;

        var compositeKey = Compose(keyPath, valueName);
        _store[compositeKey] = value;

        // Also ensure the key itself is marked as existing.
        var keyMarker = Compose(keyPath, KeyMarker);
        _store.TryAdd(keyMarker, true);
    }

    /// <inheritdoc/>
    public async Task<bool> KeyExistsAsync(string keyPath)
    {
        await Task.CompletedTask;

        var keyMarker = Compose(keyPath, KeyMarker);
        return _store.ContainsKey(keyMarker);
    }

    /// <inheritdoc/>
    public async Task DeleteValueAsync(string keyPath, string valueName)
    {
        await Task.CompletedTask;

        var compositeKey = Compose(keyPath, valueName);
        _store.TryRemove(compositeKey, out _);
    }
}
