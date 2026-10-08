using System;
using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Client.Abstractions.ActionEffects;
using Mississippi.Inlet.Client.Abstractions.State;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Exercises the default command endpoint and permits an override test.</summary>
internal class CommandUrlEffect
    : CommandActionEffectBase<CommandUrlAction, CommandUrlRequest, ProjectionsFeatureState, CommandUrlExecutingAction,
        CommandUrlSucceededAction, CommandUrlFailedAction>
{
    /// <summary>Initializes a new instance of the <see cref="CommandUrlEffect" /> class.</summary>
    /// <param name="httpClient">The transport.</param>
    /// <param name="mapper">The request mapper.</param>
    /// <param name="timeProvider">The deterministic clock.</param>
    public CommandUrlEffect(
        HttpClient httpClient,
        IMapper<CommandUrlAction, CommandUrlRequest> mapper,
        TimeProvider timeProvider
    )
        : base(httpClient, mapper, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string AggregateRoutePrefix => "/api/aggregates/customer";

    /// <inheritdoc />
    protected override string Route => "submit";
}