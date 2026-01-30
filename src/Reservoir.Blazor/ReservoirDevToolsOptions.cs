using System;
using System.Collections.Generic;
using System.Text.Json;

using Mississippi.Reservoir.Abstractions.Actions;


namespace Mississippi.Reservoir.Blazor;

/// <summary>
///     Configuration options for Reservoir Redux DevTools integration.
/// </summary>
/// <remarks>
///     Justification: public to enable application-level configuration via options.
/// </remarks>
public sealed class ReservoirDevToolsOptions
{
    /// <summary>
    ///     Gets or sets the enablement mode for DevTools integration.
    /// </summary>
    public ReservoirDevToolsEnablement Enablement { get; set; } = ReservoirDevToolsEnablement.Off;

    /// <summary>
    ///     Gets or sets the instance name shown in DevTools.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of actions to retain in DevTools history.
    /// </summary>
    public int? MaxAge { get; set; }

    /// <summary>
    ///     Gets or sets the batching latency in milliseconds for DevTools messages.
    /// </summary>
    public int? Latency { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether DevTools should auto-pause when not open.
    /// </summary>
    public bool? AutoPause { get; set; }

    /// <summary>
    ///     Gets additional DevTools options to pass through to the extension.
    /// </summary>
    public IDictionary<string, object?> AdditionalOptions { get; } = new Dictionary<string, object?>();

    /// <summary>
    ///     Gets or sets the action sanitizer applied before sending to DevTools.
    /// </summary>
    public Func<IAction, object?>? ActionSanitizer { get; set; }

    /// <summary>
    ///     Gets or sets the state sanitizer applied before sending to DevTools.
    /// </summary>
    public Func<IReadOnlyDictionary<string, object>, object?>? StateSanitizer { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether time-travel state rehydration should be strict.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, time-travel operations (jump, reset, import) will only succeed
    ///         if ALL registered feature states can be successfully deserialized from the incoming payload.
    ///         If any feature state is missing or fails deserialization, the entire operation is rejected
    ///         and the current state remains unchanged.
    ///     </para>
    ///     <para>
    ///         When <see langword="false"/> (default), time-travel uses best-effort rehydration: features
    ///         that cannot be matched or deserialized are skipped, and the rest are applied.
    ///     </para>
    /// </remarks>
    public bool IsStrictStateRehydrationEnabled { get; set; }

    /// <summary>
    ///     Gets the JSON serializer options used for time-travel state rehydration.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };
}

/// <summary>
///     Defines how Reservoir DevTools integration is enabled.
/// </summary>
/// <remarks>
///     Justification: public to enable application-level configuration via options.
/// </remarks>
public enum ReservoirDevToolsEnablement
{
    /// <summary>
    ///     DevTools integration is disabled.
    /// </summary>
    Off = 0,

    /// <summary>
    ///     DevTools integration is enabled only in Development environments.
    /// </summary>
    DevelopmentOnly = 1,

    /// <summary>
    ///     DevTools integration is enabled in all environments.
    /// </summary>
    Always = 2,
}
