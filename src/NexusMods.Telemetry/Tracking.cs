using JetBrains.Annotations;

namespace NexusMods.Telemetry;

/// <summary>
/// Tracking methods.
/// </summary>
/// <remarks>
/// Metal Mod Manager sends no usage data, so these calls do nothing. They stay so that existing call sites compile.
/// </remarks>
[PublicAPI]
[Obsolete]
public static class Tracking
{
    /// <summary>
    /// Check whether tracking is enabled to gate computational expensive operations. Always false.
    /// </summary>
    public static bool IsEnabled => false;

    /// <summary>
    /// Track an event. Does nothing.
    /// </summary>
    public static void AddEvent(EventDefinition definition, EventMetadata metadata) { }
}
