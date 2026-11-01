using NexusMods.Sdk;

namespace NexusMods.Networking.NexusWebApi;

internal static class ClientConfig
{
    private const string DefaultBaseDomain = "nexusmods.com";
    private const string DefaultApiSubdomain = "api";
    private const string DefaultUsersSubdomain = "users";

    // The OAuth client ID Nexus Mods issues to Metal Mod Manager when it registers the app. Until then there is
    // none, and signing in is unavailable: using another app's client ID (such as the Nexus Mods App's "nma")
    // impersonates that app, which the Nexus Mods API Acceptable Use Policy forbids.
    // Testers use a personal API key instead (NEXUS_API_KEY).
    private const string? DefaultOAuthClientId = null;

    static ClientConfig()
    {
        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsBaseDomain, out var baseDomain))
            baseDomain = DefaultBaseDomain;
        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsApiSubdomain, out var apiSubdomain))
            apiSubdomain = DefaultApiSubdomain;
        if (!EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsUsersSubdomain, out var usersSubdomain))
            usersSubdomain = DefaultUsersSubdomain;

        BaseUrl = new Uri($"https://{baseDomain}");
        ApiUrl = new Uri($"https://{apiSubdomain}.{baseDomain}");
        UsersUrl = new Uri($"https://{usersSubdomain}.{baseDomain}");
        LegacyApiEndpoint = new Uri($"https://{apiSubdomain}.{baseDomain}/v1");
        GraphQlEndpoint = new Uri($"https://{apiSubdomain}.{baseDomain}/v2/graphql");
        OAuthUrl = new Uri($"{UsersUrl}oauth");

        OAuthClientId = EnvironmentVariables.TryGetString(EnvironmentVariableNames.NexusModsOAuthClientId, out var clientId)
            ? clientId
            : DefaultOAuthClientId;
    }

    public static readonly Uri BaseUrl;
    public static readonly Uri ApiUrl;
    public static readonly Uri UsersUrl;
    public static readonly Uri LegacyApiEndpoint;
    public static readonly Uri GraphQlEndpoint;
    public static readonly Uri OAuthUrl;

    /// <summary>
    /// The OAuth client ID, or null while the app has none and signing in is unavailable.
    /// </summary>
    public static readonly string? OAuthClientId;
}
