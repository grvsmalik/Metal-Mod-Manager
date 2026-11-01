using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;

namespace NexusMods.Networking.NexusWebApi;

internal class HandlerRegistration : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IOSInterop _osInterop;
    private readonly ISettingsManager _settingsManager;

    public HandlerRegistration(
        ILogger<HandlerRegistration> logger,
        IOSInterop osInterop,
        ISettingsManager settingsManager)
    {
        _logger = logger;
        _osInterop = osInterop;
        _settingsManager = settingsManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settingsManager.Get<NxmHandlerSettings>().RegisterAsNxmHandler)
        {
            _logger.LogInformation("Not registering as the handler for nxm links: the setting is off");
            return;
        }

        try
        {
            await _osInterop.RegisterUriSchemeHandler(scheme: "nxm", cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while registering handler for nxm links");
        }
    }
}
