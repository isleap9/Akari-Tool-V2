// IMemoryManager — abstracts PowerShell MMAgent commands for testability.
//
// The production MemoryManager wraps PowerShell invocation of Disable-MMAgent /
// Enable-MMAgent. The FakeMemoryManager is an in-memory implementation for unit tests.
// This abstraction is necessary because MMAgent operations require admin rights
// and real Windows system components — cannot be tested in unit tests without it.

namespace Akari.Engine.Memory;

/// <summary>
/// Result of a memory operation.
/// </summary>
public class MemoryOperationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Output from the PowerShell command (if any).</summary>
    public string? Output { get; init; }

    /// <summary>Error message if the operation failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Whether memory compression was previously enabled (for revert).</summary>
    public bool PreviousState { get; init; }
}

/// <summary>
/// Abstraction over Windows memory management via PowerShell MMAgent cmdlets.
/// Allows injecting a fake for unit testing (production uses real PowerShell).
/// </summary>
public interface IMemoryManager
{
    /// <summary>
    /// Disables Windows memory compression.
    /// Corresponds to: Disable-MMAgent -MemoryCompression
    /// </summary>
    Task<MemoryOperationResult> DisableCompressionAsync();

    /// <summary>
    /// Enables Windows memory compression.
    /// Corresponds to: Enable-MMAgent -MemoryCompression
    /// </summary>
    Task<MemoryOperationResult> EnableCompressionAsync();

    /// <summary>
    /// Checks whether memory compression is currently enabled.
    /// Corresponds to: Get-MMAgent
    /// </summary>
    Task<bool> IsCompressionEnabledAsync();
}
