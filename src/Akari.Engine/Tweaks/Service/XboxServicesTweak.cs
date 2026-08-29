// SVC-01: Xbox Background Services tweak.
// Disables Xbox-related background services to free system resources during gaming.
//
// Services (from AkariOS Tweaks/6 Windows/19 Gamebar.ps1 §servicesoff, lines 196-218):
// - XblAuthManager — Xbox Live Auth Manager
// - XblGameSave — Xbox Live Game Save
// - XboxGipSvc — Xbox Game Input
// - XboxNetApiSvc — Xbox Networking
// - GamingServices / GamingServicesNet — Gaming Services
// - BcastDVRUserService — GameDVR/Broadcast User Service
// - GameInputSvc — GameInput Service
//
// Apply: Write Start=4 (disabled) to each service's registry key + Stop service
// Revert: Write Start=3 (manual) to each service's registry key + Start service
// Source: AkariOS Tweaks/6 Windows/19 Gamebar.ps1 and 8 Advanced/17 Services.ps1

using Akari.Engine.Core.Models;

namespace Akari.Engine.Tweaks.Service;

/// <summary>
/// Xbox Background Services (SVC-01): Disables background Xbox services that
/// consume resources during gaming — Xbox Live Auth Manager, Xbox Live Game Save,
/// Xbox Game Input, Xbox Networking, Gaming Services, GameDVR/Broadcast, and GameInput.
/// </summary>
public class XboxServicesTweak
{
    public static TweakDefinition Definition => new()
    {
        Id = "SVC-01",
        Name = "Xbox Background Services",
        Category = "Services",
        Type = TweakType.Service,
        Description = "Disables Xbox background services (Xbox Live Auth Manager, Xbox Live Game Save, " +
                      "Xbox Game Input, Xbox Networking, Gaming Services, GameDVR/Broadcast, GameInput) " +
                      "to free system resources during gaming.",
        ServiceNames = new List<string>
        {
            "XblAuthManager",       // Xbox Live Auth Manager
            "XblGameSave",          // Xbox Live Game Save
            "XboxGipSvc",           // Xbox Game Input
            "XboxNetApiSvc",        // Xbox Networking
            "GamingServices",       // Gaming Services
            "BcastDVRUserService",  // GameDVR/Broadcast User Service
            "GameInputSvc",         // GameInput Service
        },
        ServiceStartValue = "4",          // Disable (Start=4)
        ServiceRevertStartValue = "3",    // Manual (Start=3, default)
        RequiresRestart = false,
        RequiresAdmin = true,
        SortOrder = 1,
    };
}