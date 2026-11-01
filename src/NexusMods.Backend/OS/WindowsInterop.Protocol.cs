using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace NexusMods.Backend.OS;

internal partial class WindowsInterop
{
    [SupportedOSPlatformGuard("windows")]
    private bool IsWindows() => _os.IsWindows;

    public ValueTask RegisterUriSchemeHandler(string scheme, bool setAsDefaultHandler = true, CancellationToken cancellationToken = default)
    {
        if (!IsWindows()) return ValueTask.CompletedTask;

        // NOTE(erri120): See this comment for an in-depth guide on protocol handlers:
        // https://github.com/Nexus-Mods/NexusMods.App/pull/1691#issuecomment-2194418849
        // We've decided use the same method that Vortex and MO2 use, which is using a
        // generic ProgID. This means we're always overwriting the existing values.

        try
        {
            SetAsDefaultHandler(scheme);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while updating registry to register protocol handler for `{Scheme}`", scheme);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<IDisposable> RegisterUriSchemeHandlerTemporarily(string scheme, CancellationToken cancellationToken = default)
    {
        if (!IsWindows()) return R3.Disposable.Empty;

        ProtocolHandlerState? previous = null;
        try
        {
            previous = ReadHandlerState(scheme);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while reading the current handler for `{Scheme}`; it won't be restored", scheme);
        }

        await RegisterUriSchemeHandler(scheme, setAsDefaultHandler: true, cancellationToken);
        return R3.Disposable.Create(() =>
        {
            try
            {
                RestoreHandlerState(scheme, previous);
                _logger.LogInformation("Restored the previous handler for `{Scheme}`", scheme);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while restoring the previous handler for `{Scheme}`", scheme);
            }
        });
    }

    /// <summary>
    /// The values a protocol handler writes under <c>HKCU\SOFTWARE\Classes\{scheme}</c>; Vortex and MO2 use the same values.
    /// </summary>
    private record ProtocolHandlerState(object? Name, object? UrlProtocol, object? Command, object? WorkingDirectory);

    [SupportedOSPlatform("windows")]
    private static ProtocolHandlerState? ReadHandlerState(string scheme)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@$"SOFTWARE\Classes\{scheme}");
        if (key is null) return null;

        using var commandKey = key.OpenSubKey(@"shell\open\command");
        return new ProtocolHandlerState(
            Name: key.GetValue(""),
            UrlProtocol: key.GetValue("URL Protocol"),
            Command: commandKey?.GetValue(""),
            WorkingDirectory: commandKey?.GetValue("WorkingDirectory")
        );
    }

    [SupportedOSPlatform("windows")]
    private static void RestoreHandlerState(string scheme, ProtocolHandlerState? state)
    {
        var path = @$"SOFTWARE\Classes\{scheme}";
        if (state is null)
        {
            // Nothing handled the scheme before we registered.
            Registry.CurrentUser.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
            return;
        }

        using var key = Registry.CurrentUser.CreateSubKey(path);
        SetOrDelete(key, "", state.Name);
        SetOrDelete(key, "URL Protocol", state.UrlProtocol);

        using var commandKey = key.CreateSubKey(@"shell\open\command");
        SetOrDelete(commandKey, "", state.Command);
        SetOrDelete(commandKey, "WorkingDirectory", state.WorkingDirectory);

        static void SetOrDelete(RegistryKey key, string name, object? value)
        {
            if (value is null) key.DeleteValue(name, throwOnMissingValue: false);
            else key.SetValue(name, value);
        }
    }

    private static string CreateProgId(string uriScheme) => $"NexusMods.App.{uriScheme}";

    [SupportedOSPlatform("windows")]
    private void RegisterApplication(string uriScheme)
    {
        // https://learn.microsoft.com/en-us/windows/win32/shell/default-programs

        const string capabilitiesPath = @"SOFTWARE\Nexus Mods\NexusMods.App\Capabilities";

        using var key = Registry.CurrentUser.CreateSubKey(capabilitiesPath);
        key.SetValue("ApplicationName", "Nexus Mods App");
        key.SetValue("ApplicationDescription", "Mod Manager for your games");

        using var urlAssociationsKey = key.CreateSubKey("UrlAssociations");
        urlAssociationsKey.SetValue(uriScheme, CreateProgId(uriScheme));

        using var registeredApplicationsKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\RegisteredApplications");
        registeredApplicationsKey.SetValue("NexusMods.App", capabilitiesPath);

        CreateProgIdClass(CreateProgId(uriScheme), $"Nexus Mods App {uriScheme.ToUpperInvariant()} Handler", isProtocolHandler: false);
    }

    [SupportedOSPlatform("windows")]
    private void CreateProgIdClass(string progId, string name, bool isProtocolHandler)
    {
        // https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/platform-apis/aa767914(v=vs.85)

        using var key = Registry.CurrentUser.CreateSubKey(@$"SOFTWARE\Classes\{progId}");
        key.SetValue("", name);
        if (isProtocolHandler) key.SetValue("URL Protocol", "");

        using var commandKey = key.CreateSubKey(@"shell\open\command");

        var executable = GetRunningExecutablePath(out _);
        commandKey.SetValue("", $"\"{executable.ToNativeSeparators(_fileSystem.OS)}\" \"%1\"");

        // NOTE(erri120): can't set the working directory for generic protocol handlers
        // due to possible issues with Vortex/MO2.
        if (!isProtocolHandler) commandKey.SetValue("WorkingDirectory", $"\"{executable.Parent.ToNativeSeparators(_fileSystem.OS)}\"");
    }

    [SupportedOSPlatform("windows")]
    private void SetAsDefaultHandler(string uriScheme)
    {
        CreateProgIdClass(uriScheme, $"Nexus Mods App {uriScheme.ToUpperInvariant()} Handler", isProtocolHandler: true);
    }
}
