// RegistryProviderTests — tracer test for the registry provider abstraction.
//
// FakeRegistryProvider tests are logic-only — they CANNOT catch UnauthorizedAccessException.
// Per D-04 and PITFALLS.md Pitfall 2: runtime ACL verification requires an actual elevated
// app launch + log file check at %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.

using Microsoft.Win32;
using Xunit;

namespace Akari.Engine.Registry;

/// <summary>
/// Tracer-level tests verifying the registry provider abstraction round-trip
/// via FakeRegistryProvider (in-memory). These validate logic only (D-04).
/// </summary>
public class RegistryProviderTests
{
    /// <summary>
    /// Tracer test: SetValueAsync then GetValueAsync returns the written value.
    /// Uses FakeRegistryProvider (logic-only, no ACL enforcement per D-04).
    /// </summary>
    [Fact]
    public async Task FakeRegistryProvider_RoundTrip_SetsAndGetsValue()
    {
        // Arrange
        var provider = new FakeRegistryProvider();
        var keyPath = @"HKLM:\SOFTWARE\Akari\Test";
        var valueName = "TestValue";
        var expected = 42;

        // Act
        await provider.SetValueAsync(keyPath, valueName, expected, RegistryValueKind.DWord);
        var actual = await provider.GetValueAsync<int>(keyPath, valueName);

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Tracer test: DeleteValueAsync removes a previously set value.
    /// </summary>
    [Fact]
    public async Task FakeRegistryProvider_DeleteValue_RemovesValue()
    {
        // Arrange
        var provider = new FakeRegistryProvider();
        var keyPath = @"HKLM:\SOFTWARE\Akari\Test";
        var valueName = "DeleteMe";
        await provider.SetValueAsync(keyPath, valueName, "hello", RegistryValueKind.String);

        // Act
        await provider.DeleteValueAsync(keyPath, valueName);
        var result = await provider.GetValueAsync<string>(keyPath, valueName);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Unit test: KeyExistsAsync returns false for a key that was never created.
    /// </summary>
    [Fact]
    public async Task FakeRegistryProvider_KeyExistsAsync_ReturnsFalseForMissingKey()
    {
        // Arrange
        var provider = new FakeRegistryProvider();

        // Act
        var exists = await provider.KeyExistsAsync(@"HKLM:\SOFTWARE\Akari\Missing");

        // Assert
        Assert.False(exists);
    }

    /// <summary>
    /// Unit test: KeyExistsAsync returns true after SetValueAsync has been called.
    /// </summary>
    [Fact]
    public async Task FakeRegistryProvider_KeyExistsAsync_ReturnsTrueAfterSetValue()
    {
        // Arrange
        var provider = new FakeRegistryProvider();
        var keyPath = @"HKLM:\SOFTWARE\Akari\Test";

        // Act
        await provider.SetValueAsync(keyPath, "SomeValue", 1, RegistryValueKind.DWord);
        var exists = await provider.KeyExistsAsync(keyPath);

        // Assert
        Assert.True(exists);
    }

    /// <summary>
    /// Unit test: SetValueAsync overwrites an existing value rather than creating a duplicate.
    /// </summary>
    [Fact]
    public async Task FakeRegistryProvider_SetValueAsync_OverwritesExistingValue()
    {
        // Arrange
        var provider = new FakeRegistryProvider();
        var keyPath = @"HKLM:\SOFTWARE\Akari\Test";
        var valueName = "OverwriteMe";
        await provider.SetValueAsync(keyPath, valueName, "first", RegistryValueKind.String);

        // Act
        await provider.SetValueAsync(keyPath, valueName, "second", RegistryValueKind.String);
        var result = await provider.GetValueAsync<string>(keyPath, valueName);

        // Assert
        Assert.Equal("second", result);
    }
}
