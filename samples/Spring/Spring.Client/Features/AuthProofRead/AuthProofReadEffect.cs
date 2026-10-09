using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.AuthSimulation;
using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;
using MississippiSamples.Spring.Client.Features.AuthSimulation;


namespace MississippiSamples.Spring.Client.Features.AuthProofRead;

/// <summary>Fetches the real protected projection while preserving the originating request identity.</summary>
internal sealed class AuthProofReadEffect : ActionEffectBase<ReadAuthProofProjectionAction, AuthProofReadState>
{
    /// <summary>Initializes a new instance of the <see cref="AuthProofReadEffect" /> class.</summary>
    /// <param name="fetcher">The registered Inlet projection fetcher.</param>
    /// <param name="headers">The local HTTP persona handler.</param>
    public AuthProofReadEffect(
        IProjectionFetcher fetcher,
        AuthSimulationHeadersHandler headers
    )
    {
        Fetcher = fetcher;
        Headers = headers;
    }

    private IProjectionFetcher Fetcher { get; }

    private AuthSimulationHeadersHandler Headers { get; }

    /// <inheritdoc />
    public override async IAsyncEnumerable<IAction> HandleAsync(
        ReadAuthProofProjectionAction action,
        AuthProofReadState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        _ = currentState;
        ProjectionFetchResult? result = null;
        string? error = null;
        bool cancelled = false;

        // The guard and FetchAsync run without an intervening await. AutoProjectionFetcher
        // applies the handler's current headers synchronously when it sends the request.
        if (!IsCurrentHttpPersona(action.Persona))
        {
            error = "The persona changed before this read started. Refresh protected read.";
        }
        else
        {
            try
            {
                result = await Fetcher.FetchAsync(typeof(AuthProofProjectionDto), action.EntityId, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                error = exception.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "HTTP 401: this request has no authenticated identity.",
                    HttpStatusCode.Forbidden => "HTTP 403: this request lacks the required claim policy.",
                    var _ => $"Protected read failed: {exception.Message}",
                };
            }
            catch (JsonException exception)
            {
                error = $"The projection response could not be read: {exception.Message}";
            }
            catch (OperationCanceledException)
            {
                cancelled = cancellationToken.IsCancellationRequested;
                error = "The protected read timed out. Refresh to try again.";
            }
        }

        if (cancelled)
        {
            yield break;
        }

        if (error is null && result is null)
        {
            error = "No projection fetcher is registered for this read.";
        }

        yield return new AuthProofProjectionReadCompletedAction(
            action.RequestId,
            result?.Data as AuthProofProjectionDto,
            result?.Version ?? -1,
            error);
    }

    private bool IsCurrentHttpPersona(
        AuthSimulationState persona
    ) =>
        (Headers.IsAnonymous == persona.IsAnonymous) &&
        string.Equals(Headers.Roles, persona.Roles, StringComparison.Ordinal) &&
        string.Equals(Headers.Claims, persona.Claims, StringComparison.Ordinal);
}