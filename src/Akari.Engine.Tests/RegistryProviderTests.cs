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
}
