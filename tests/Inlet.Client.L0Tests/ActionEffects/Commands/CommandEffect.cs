using System;
using System.Collections.Generic;
using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Client.Abstractions.ActionEffects;
using Mississippi.Inlet.Client.L0Tests.Helpers;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Exercises the public command effect contract with test lifecycle actions.
/// </summary>
internal sealed class CommandEffect
    : CommandActionEffectBase<CommandEffectAction, Dictionary<string, string>, TestAggregateState,
        CommandEffectExecutingAction, CommandEffectSucceededAction, CommandEffectFailedAction>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandEffect" /> class.
    /// </summary>
    /// <param name="httpClient">The in-memory HTTP client.</param>
    /// <param name="mapper">The request mapper.</param>
    /// <param name="timeProvider">The optional clock.</param>
    public CommandEffect(
        HttpClient httpClient,
        IMapper<CommandEffectAction, Dictionary<string, string>> mapper,
        TimeProvider? timeProvider = null
    )
        : base(httpClient, mapper, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string AggregateRoutePrefix => "/api/aggregates/test";

    /// <inheritdoc />
    protected override string Route => "submit";

    /// <summary>
    ///     Gets the clock selected by the base constructor.
    /// </summary>
    internal TimeProvider Clock => TimeProvider;
}