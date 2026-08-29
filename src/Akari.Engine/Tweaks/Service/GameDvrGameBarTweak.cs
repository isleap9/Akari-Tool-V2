// SVC-02: GameDVR / Game Bar tweak.
// Disables GameDVR and GameBar recording/background features.
//
// Registry values (from AkariOS Tweaks/6 Windows/19 Gamebar.ps1 §gamebaroff, lines 90-135):
// Apply (disable):
// - HKCU\System\GameConfigStore\GameDVR_Enabled = 0 (DWORD)
// - HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR\AppCaptureEnabled = 0 (DWORD)
// - HKCU\Software\Microsoft\GameBar\UseNexusForGameBarEnabled = 0 (DWORD)
// - HKCU\Software\Microsoft\GameBar\GamepadNexusChordEnabled = 0 (DWORD)
// - HKLM\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter\ActivationType = 0 (DWORD)
// - Service: BcastDVRUserService, GameInputSvc, XboxGipSvc, XblAuthManager, XblGameSave, XboxNetApiSvc Start=4
//
// Revert (enable/restore):
// - GameDVR_Enabled = 0 (default)
// - AppCaptureEnabled = delete (restore default)
// - UseNexusForGameBarEnabled = delete
// - GamepadNexusChordEnabled = delete
// - ActivationType = 1
// - Service Start=3 (manual)
// Source: AkariOS Tweaks/6 Windows/19 Gamebar.ps1

using Akari.Engine.Core.Models;
using Microsoft.Win32;

namespace Akari.Engine.Tweaks.Service;

/// <summary>
/// GameDVR / Game Bar (SVC-02): Disables GameDVR recording and GameBar Xbox overlay
/// via registry values in HKCU and HKLM, plus stops the BcastDVRUserService,
/// GameInputSvc, XboxGipSvc, XblAuthManager, XblGameSave, and XboxNetApiSvc services.
/// </summary>
public class GameDvrGameBarTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "SVC-02",
        Name = "GameDVR & Game Bar",
        Category = "Services",
        Type = TweakType.Service,
        Description = "Disables GameDVR screen recording and the Xbox Game Bar overlay " +
                      "(UseNexusForGameBar, GamepadNexusChord, AppCaptureEnabled, " +
                      "PresenceServer ActivationType). Also stops related services.",
        // Service names managed by this tweak (from Gamebar.ps1 gamebaroff, lines 196-218)
        ServiceNames = new List<string>
        {
            "BcastDVRUserService",    // GameDVR/Broadcast User Service
            "GameInputSvc",           // GameInput Service
            "XboxGipSvc",             // Xbox Game Input
            "XblAuthManager",         // Xbox Live Auth Manager
            "XblGameSave",            // Xbox Live Game Save
            "XboxNetApiSvc",          // Xbox Networking
        },
        ServiceStartValue = "4",          // Disable (Start=4)
        ServiceRevertStartValue = "3",    // Manual (Start=3, default)
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 2,

        // Registry values to apply (disable GameDVR/GameBar)
        // These are applied via IRegistryProvider during ApplyAsync
        RegistryMultiValues = new List<RegistryMultiValue>
        {
            // HKCU: Disable GameDVR
            new RegistryMultiValue
            {
                Key = @"HKCU:\System\GameConfigStore",
                ValueName = "GameDVR_Enabled",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
            // HKCU: Disable GameDVR app capture
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\GameDVR",
                ValueName = "AppCaptureEnabled",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
            // HKCU: Disable GameBar open with controller
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\GameBar",
                ValueName = "UseNexusForGameBarEnabled",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
            // HKCU: Disable View+Menu as guide button
            new RegistryMultiValue
            {
                Key = @"HKCU:\Software\Microsoft\GameBar",
                ValueName = "GamepadNexusChordEnabled",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
            // HKLM: Disable GameBar PresenceServer activation (requires admin)
            new RegistryMultiValue
            {
                Key = @"HKLM:\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter",
                ValueName = "ActivationType",
                ValueData = "0",
                ValueKind = RegistryValueKind.DWord,
            },
        },
    };
}