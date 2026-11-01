using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk;
using NexusMods.Sdk.Jobs;
using R3;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// helper class to deal with OAuth2 authentication messages
/// </summary>
public class OAuth
{
    // NOTE(erri120): The backend has a list of valid redirect URLs and client IDs.
    // We can't change these on our own.
    private const string OAuthRedirectUrl = "nxm://oauth/callback";

    private readonly IJobMonitor _jobMonitor;
    private readonly ILogger<OAuth> _logger;
    private readonly HttpClient _http;
    private readonly IOSInterop _os;
    private readonly IIDGenerator _idGenerator;
    private readonly BehaviorSubject<NXMOAuthUrl?> _nxmUrlMessages = new(initialValue: null);

    /// <summary>
    /// The OAuth client ID to sign in with; see <see cref="ClientConfig.OAuthClientId"/>.
    /// </summary>
    internal string? ClientId { get; init; } = ClientConfig.OAuthClientId;

    /// <summary>
    /// Whether signing in with OAuth is possible, which needs a client ID issued to this app.
    /// </summary>
    public bool IsAvailable => ClientId is not null;

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth(
        IJobMonitor jobMonitor,
        ILogger<OAuth> logger,
        HttpClient http,
        IIDGenerator idGenerator,
        IOSInterop os)
    {
        _jobMonitor = jobMonitor;
        _logger = logger;
        _http = http;
        _os = os;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Make an authorization request
    /// </summary>
    public async Task<JwtTokenReply?> AuthorizeRequest(CancellationToken cancellationToken)
    {
        if (ClientId is null)
        {
            _logger.LogWarning("Can't sign in to Nexus Mods: this app has no OAuth client ID yet");
            return null;
        }

        // The website sends the login result back through an nxm:// link. Handle nxm links only while
        // signing in, so the user's usual handler (for example Vortex) gets them back afterwards.
        using var handlerRegistration = await _os.RegisterUriSchemeHandlerTemporarily("nxm", cancellationToken);

        var job = OAuthJob.Create(
            jobMonitor: _jobMonitor,
            idGenerator: _idGenerator,
            os: _os,
            httpClient: _http,
            clientId: ClientId,
            nxmUrlMessages: _nxmUrlMessages.WhereNotNull()
        );

        var res = await job;
        return res.ValueOrDefault();
    }
    
    /// <summary>
    /// Add a new url as a response to an OAuth request
    /// </summary>
    public void AddUrl(NXMOAuthUrl url)
    {
        _nxmUrlMessages.OnNext(url);
    }

    /// <summary>
    /// request a new access token
    /// </summary>
    /// <param name="refreshToken">the refresh token</param>
    /// <param name="cancel"></param>
    /// <returns>a new token reply</returns>
    public async Task<JwtTokenReply?> RefreshToken(string refreshToken, CancellationToken cancel)
    {
        if (ClientId is null)
        {
            _logger.LogWarning("Can't refresh the Nexus Mods sign-in: this app has no OAuth client ID yet");
            return null;
        }

        var request = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", ClientId },
            { "refresh_token", refreshToken },
        };

        var content = new FormUrlEncodedContent(request);

        var response = await _http.PostAsync($"{ClientConfig.OAuthUrl}/token", content, cancel);
        var responseString = await response.Content.ReadAsStringAsync(cancel);
        return JsonSerializer.Deserialize<JwtTokenReply>(responseString);
    }

    internal static async Task<JwtTokenReply?> AuthorizeToken(
        string clientId,
        string verifier,
        string code,
        HttpClient httpClient,
        CancellationToken cancel)
    {
        var request = new Dictionary<string, string> {
            { "grant_type", "authorization_code" },
            { "client_id", clientId },
            { "redirect_uri", OAuthRedirectUrl },
            { "code", code },
            { "code_verifier", verifier },
        };

        var content = new FormUrlEncodedContent(request);

        var response = await httpClient.PostAsync($"{ClientConfig.OAuthUrl}/token", content, cancel);
        var responseString = await response.Content.ReadAsStringAsync(cancel);
        return JsonSerializer.Deserialize<JwtTokenReply>(responseString);
    }

    internal static Uri GenerateAuthorizeUrl(string clientId, string challenge, string state)
    {
        var request = new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "scope", "openid profile email" },
            { "code_challenge_method", "S256" },
            { "client_id", clientId },
            { "redirect_uri",  OAuthRedirectUrl },
            { "code_challenge", challenge },
            { "state", state },
        };

        return new Uri(QueryHelpers.AddQueryString($"{ClientConfig.OAuthUrl}/authorize", request));
    }
}
