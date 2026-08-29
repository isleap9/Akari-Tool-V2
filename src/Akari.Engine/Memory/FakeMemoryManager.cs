// FakeMemoryManager — in-memory implementation for unit tests.
//
// CRITICAL (D-04, PITFALLS.md Pitfall 2):
// This manager does NOT invoke real PowerShell or MMAgent. It is an in-memory
// boolean tracker with NO actual system interaction. Unit tests using this fake
// validate LOGIC ONLY. Runtime verification of actual memory compression operations
// must be performed via an elevated app launch and log file inspection at
// %LOCALAPPDATA%\Akari\App\logs\app-YYYY-MM-DD.log.

namespace Akari.Engine.Memory;

/// <summary>
/// In-memory implementation of <see cref="IMemoryManager"/> for unit tests.
/// Does NOT invoke real PowerShell or MMAgent — logic-only validation
/// (per D-04, PITFALLS.md Pitfall 2). Runtime memory operations require an
/// actual elevated launch + log check.
/// </summary>
public class FakeMemoryManager : IMemoryManager
{
    private bool _compressionEnabled = true; // Default: memory compression is enabled on Windows
    private bool _disableCalled;
    private bool _enableCalled;

    /// <summary>
    /// Initializes the fake with a known initial compression state.
    /// </summary>
    public FakeMemoryManager(bool initialCompressionState = true)
    {
        _compressionEnabled = initialCompressionState;
    }

    /// <inheritdoc/>
    public Task<MemoryOperationResult> DisableCompressionAsync()
    {
        _disableCalled = true;
        var wasEnabled = _compressionEnabled;
        _compressionEnabled = false;
        return Task.FromResult(new MemoryOperationResult
        {
            Success = true,
            Output = "MemoryCompression disabled.",
            PreviousState = wasEnabled
        });
    }

    /// <inheritdoc/>
    public Task<MemoryOperationResult> EnableCompressionAsync()
    {
        _enableCalled = true;
        var wasEnabled = _compressionEnabled;
        _compressionEnabled = true;
        return Task.FromResult(new MemoryOperationResult
        {
            Success = true,
            Output = "MemoryCompression enabled.",
            PreviousState = wasEnabled
        });
    }

    /// <inheritdoc/>
    public Task<bool> IsCompressionEnabledAsync()
    {
        return Task.FromResult(_compressionEnabled);
    }

    /// <summary>
    /// Verifies that DisableCompressionAsync was called.
    /// </summary>
    public bool WasDisableCalled => _disableCalled;

    /// <summary>
    /// Verifies that EnableCompressionAsync was called.
    /// </summary>
    public bool WasEnableCalled => _enableCalled;

    /// <summary>
    /// Returns the current compression state.
    /// </summary>
    public bool CurrentCompressionEnabled => _compressionEnabled;
}
