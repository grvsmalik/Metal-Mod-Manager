using JetBrains.Annotations;
using NexusMods.Sdk.Settings;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Settings for handling <c>nxm://</c> download links from the Nexus Mods website.
/// </summary>
public record NxmHandlerSettings : ISettings
{
    /// <summary>
    /// When enabled, the app registers itself as the system handler for <c>nxm://</c> links on startup,
    /// replacing whichever manager (for example Vortex) handled them before. Off by default.
    /// </summary>
    public bool RegisterAsNxmHandler { get; [UsedImplicitly] set; }

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureBackend(StorageBackendOptions.Use(StorageBackends.Json))
            .ConfigureProperty(
                x => x.RegisterAsNxmHandler,
                new PropertyOptions<NxmHandlerSettings, bool>
                {
                    Section = Sections.General,
                    DisplayName = "Handle Nexus Mods download links",
                    DescriptionFactory = _ => "Open \"Mod Manager Download\" links from the Nexus Mods website in this app. " +
                                              "This replaces the mod manager that handles them now, such as Vortex. " +
                                              "To hand them back, turn this off and start the other mod manager.",
                    RequiresRestart = true,
                },
                new BooleanContainerOptions()
            );
    }
}
