using R3;

namespace NexusMods.App.UI.Overlays;

public interface IWelcomeOverlayViewModel : IOverlayViewModel
{
    ReactiveCommand CommandOpenDiscord { get; }
    ReactiveCommand CommandOpenForum { get; }
    ReactiveCommand CommandOpenGitHub { get; }

    ReactiveCommand<Unit> CommandLogIn { get; }
    ReactiveCommand<Unit> CommandLogOut { get; }
    IReadOnlyBindableReactiveProperty<bool> IsLoggedIn { get; }

    /// <summary>
    /// Whether signing in to Nexus Mods is possible in this build.
    /// </summary>
    bool IsLoginAvailable { get; }

    ReactiveCommand CommandClose { get; }
}
