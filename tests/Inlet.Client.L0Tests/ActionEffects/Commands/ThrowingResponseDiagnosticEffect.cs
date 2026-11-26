using System;
using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Provides failing diagnostics through the original externally inherited command abstraction.
/// </summary>
internal sealed class ThrowingResponseDiagnosticEffect : CommandUrlEffect
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ThrowingResponseDiagnosticEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The in-memory HTTP client.</param>
    /// <param name="mapper">The request mapper.</param>
    /// <param name="timeProvider">The deterministic clock.</param>
    public ThrowingResponseDiagnosticEffect(
        HttpClient httpClient,
        IMapper<CommandUrlAction, CommandUrlRequest> mapper,
        TimeProvider timeProvider
    )
        : base(httpClient, mapper, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override void OnInvalidResponse(
        Exception exception,
        string commandId,
        string commandType,
        int statusCode
    ) =>
        throw new InvalidOperationException("Diagnostic hook failed.");
}