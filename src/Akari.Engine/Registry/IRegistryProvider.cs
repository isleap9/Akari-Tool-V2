// Registry provider abstraction for Akari Engine.
// Provides a contract for registry access that can be implemented by the production
// Win32RegistryProvider (real HKLM writes with ACL enforcement) and FakeRegistryProvider
// (in-memory for unit tests — logic only, does NOT enforce ACLs per D-04).

using Microsoft.Win32;

namespace Akari.Engine.Registry;

/// <summary>
/// Abstraction over Windows registry access. The production implementation
/// (<see cref="Win32RegistryProvider"/>) performs real registry operations using
/// Microsoft.Win32.Registry with the 2-arg <c>OpenSubKey(path, true)</c> writable
/// overload and explicit <see cref="RegistryView.Registry64"/> for HKLM access.
/// The fake implementation (<see cref="FakeRegistryProvider"/>) is an in-memory
/// dictionary used for unit-test logic validation only — it does NOT enforce ACLs
/// and cannot surface runtime <c>UnauthorizedAccessException</c> (see PITFALLS.md
/// Pitfall 2, D-04).
/// </summary>
public interface IRegistryProvider
{
    /// <summary>
    /// Asynchronously reads a registry value of the specified type.
    /// Returns the default value for <typeparamref name="T"/> if the value or key does not exist.
    /// </summary>
    /// <typeparam name="T">The expected type of the registry value.</typeparam>
    /// <param name="keyPath">Full registry key path including hive prefix (e.g. <c>HKLM:\SOFTWARE\…</c>).</param>
    /// <param name="valueName">The name of the registry value to read.</param>
    /// <returns>The value cast to <typeparamref name="T"/>, or <c>default</c> if not found.</returns>
    Task<T?> GetValueAsync<T>(string keyPath, string valueName);

    /// <summary>
    /// Asynchronously writes a registry value, creating or overwriting it.
    /// </summary>
    /// <param name="keyPath">Full registry key path including hive prefix.</param>
    /// <param name="valueName">The name of the registry value to write.</param>
    /// <param name="value">The value data to write.</param>
    /// <param name="kind">The <see cref="RegistryValueKind"/> of the value.</param>
    Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind);

    /// <summary>
    /// Asynchronously checks whether a registry key exists at the given path.
    /// </summary>
    /// <param name="keyPath">Full registry key path including hive prefix.</param>
    /// <returns><c>true</c> if the key exists; <c>false</c> otherwise.</returns>
    Task<bool> KeyExistsAsync(string keyPath);

    /// <summary>
    /// Asynchronously deletes a named registry value from the specified key.
    /// </summary>
    /// <param name="keyPath">Full registry key path including hive prefix.</param>
    /// <param name="valueName">The name of the registry value to delete.</param>
    Task DeleteValueAsync(string keyPath, string valueName);
}
