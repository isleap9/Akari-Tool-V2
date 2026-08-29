// JsonFileStateService — persists tweak status to %LOCALAPPDATA%\Akari\App\state.json.
//
// Includes startup re-validation (D-05, ENG-06): RevalidateAsync() reads each
// persisted "Applied" tweak's actual registry value and compares against the
// expected value. Mismatches indicate Windows Update reverts — the status is
// reset to "NotApplied".
//
// Uses FakeRegistryProvider for re-validation testing (logic-only per D-04).
// Runtime ACL verification still requires elevated launch + log check.

using System.Text.Json;
using Akari.Engine.Core.Models;
using Akari.Engine.Registry;
using Akari.Engine.Storage.Models;

namespace Akari.Engine.Storage;

/// <summary>
/// JSON file-based implementation of <see cref="ITweakStateService"/>.
/// Persists to <c>%LOCALAPPDATA%\Akari\App\state.json</c> (per D-03, ENG-06).
/// </summary>
public class JsonFileStateService : ITweakStateService
{
    private readonly string _stateFilePath;
    private readonly IRegistryProvider _registryProvider;
    private readonly object _lock = new();
    private Dictionary<string, TweakStateEntry>? _cache;

    /// <summary>
    /// Initializes a new JsonFileStateService using the standard state file path.
    /// </summary>
    public JsonFileStateService(IRegistryProvider registryProvider)
    {
        _registryProvider = registryProvider;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _stateFilePath = Path.Combine(localAppData, "Akari", "App", "state.json");
    }

    /// <summary>
    /// Initializes a new JsonFileStateService with a custom state file path (for testing).
    /// </summary>
    public JsonFileStateService(IRegistryProvider registryProvider, string customStateFilePath)
    {
        _registryProvider = registryProvider;
        _stateFilePath = customStateFilePath;
    }

    /// <inheritdoc/>
    public async Task<TweakStatus> GetStatusAsync(string tweakId)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var state = LoadState();
                if (state.TryGetValue(tweakId, out var entry))
                {
                    return entry.Status;
                }
                return TweakStatus.NotApplied;
            }
        });
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(string tweakId, TweakStatus status)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                var state = LoadState();
                var now = DateTimeOffset.UtcNow;

                if (!state.TryGetValue(tweakId, out var entry))
                {
                    entry = new TweakStateEntry();
                    state[tweakId] = entry;
                }

                entry.Status = status;
                if (status == TweakStatus.Applied)
                {
                    entry.LastAppliedAt = now;
                }
                else
                {
                    entry.LastRevertedAt = now;
                }

                SaveState(state);
            }
        });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, TweakStatus>> GetAllStatusAsync()
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var state = LoadState();
                var result = new Dictionary<string, TweakStatus>();
                foreach (var kvp in state)
                {
                    result[kvp.Key] = kvp.Value.Status;
                }
                return (IReadOnlyDictionary<string, TweakStatus>)result;
            }
        });
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> RevalidateAsync(IEnumerable<TweakDefinition> tweaks)
    {
        var revertedIds = new List<string>();
        var tweaksList = tweaks.ToList();

        foreach (var tweak in tweaksList.Where(t => t.RegistryKey != null && t.RegistryValueName != null))
        {
            var currentStatus = await GetStatusAsync(tweak.Id);
            if (currentStatus != TweakStatus.Applied) continue;

            // Read the actual registry value (D-05 startup re-validation).
            // For RegistryValueKind.DWord, the FakeRegistryProvider stores the raw value.
            // The expected value comes from the tweak definition's RegistryValueData.
            if (tweak.RegistryValueKind == Microsoft.Win32.RegistryValueKind.DWord &&
                int.TryParse(tweak.RegistryValueData, out var expectedInt))
            {
                var actualInt = await _registryProvider.GetValueAsync<int>(
                    tweak.RegistryKey!, tweak.RegistryValueName!);

                if (actualInt != expectedInt)
                {
                    // Windows Update reverted the tweak — reset status to NotApplied.
                    await UpdateAsync(tweak.Id, TweakStatus.NotApplied);
                    revertedIds.Add(tweak.Id);
                }
            }
        }

        return revertedIds;
    }

    /// <summary>
    /// Loads the state dictionary from the JSON file, or returns empty if not yet created.
    /// Called within lock.
    /// </summary>
    private Dictionary<string, TweakStateEntry> LoadState()
    {
        if (_cache != null) return _cache;

        if (File.Exists(_stateFilePath))
        {
            try
            {
                var json = File.ReadAllText(_stateFilePath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, TweakStateEntry>>(json)
                    ?? new Dictionary<string, TweakStateEntry>();
            }
            catch (JsonException)
            {
                // Corrupt state file — start fresh (T-02-01 mitigation).
                _cache = new Dictionary<string, TweakStateEntry>();
            }
        }
        else
        {
            _cache = new Dictionary<string, TweakStateEntry>();
        }

        return _cache;
    }

    /// <summary>
    /// Persists the state dictionary to the JSON file.
    /// Called within lock.
    /// </summary>
    private void SaveState(Dictionary<string, TweakStateEntry> state)
    {
        var dir = Path.GetDirectoryName(_stateFilePath);
        if (dir != null) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_stateFilePath, json, System.Text.Encoding.UTF8);
    }
}
