namespace Mississippi.Common.Abstractions.Diagnostics;

/// <summary>
///     Stable diagnostic codes emitted by Mississippi builder validation.
/// </summary>
/// <remarks>
///     <para>Code format: <c>MISS-{AREA}-{NNN}</c>.</para>
///     <list type="bullet">
///         <item><c>CORE</c> — shared / cross-role composition failures.</item>
///         <item><c>CLI</c>  — client-specific failures.</item>
///         <item><c>GTW</c>  — gateway-specific failures.</item>
///         <item><c>RTM</c>  — runtime-specific failures.</item>
///     </list>
/// </remarks>
public static class MississippiDiagnosticCodes
{
    /// <summary>Client-specific duplicate attach.</summary>
    public const string ClientDuplicateAttach = "MISS-CLI-001";

    /// <summary>Duplicate terminal attach — <c>UseMississippi</c> called twice on the same host builder.</summary>
    public const string DuplicateAttach = "MISS-CORE-001";

    /// <summary>Duplicate domain registration (aggregate, projection, saga, or feature).</summary>
    public const string DuplicateRegistration = "MISS-CORE-004";

    /// <summary>Ambiguous or insufficient gateway security inference.</summary>
    public const string GatewayAmbiguousSecurity = "MISS-GTW-001";

    /// <summary>Gateway authorization mode requires a default policy but none was configured.</summary>
    public const string GatewayMissingDefaultPolicy = "MISS-GTW-002";

    /// <summary>Invalid root configuration detected during builder validation.</summary>
    public const string InvalidRootConfiguration = "MISS-CORE-003";

    /// <summary>A reducer references an event type without a corresponding <c>AddEventType</c> registration.</summary>
    public const string ReducerEventTypeMismatch = "MISS-CORE-005";

    /// <summary>Duplicate runtime attach — <c>UseMississippi</c> called twice on the same silo builder.</summary>
    public const string RuntimeDuplicateAttach = "MISS-RTM-001";

    /// <summary>Invalid nested builder graph combination on the runtime builder.</summary>
    public const string RuntimeInvalidBuilderGraph = "MISS-RTM-003";

    /// <summary>Missing storage-provider prerequisites for the selected runtime composition.</summary>
    public const string RuntimeMissingStoragePrerequisite = "MISS-RTM-002";

    /// <summary>Stream provider name mismatch detected between runtime and gateway roles.</summary>
    public const string RuntimeStreamProviderMismatch = "MISS-RTM-004";

    /// <summary>Unsupported host or builder shape passed to a <c>UseMississippi</c> entrypoint.</summary>
    public const string UnsupportedHostShape = "MISS-CORE-002";
}