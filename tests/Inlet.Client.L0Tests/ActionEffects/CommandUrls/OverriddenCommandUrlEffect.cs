using System;
using System.Net.Http;

using Mississippi.Common.Abstractions.Mapping;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Uses an application-owned endpoint instead of the default entity path.</summary>
internal sealed class OverriddenCommandUrlEffect : CommandUrlEffect
{
    /// <summary>Initializes a new instance of the <see cref="OverriddenCommandUrlEffect" /> class.</summary>
    /// <param name="httpClient">The transport.</param>
    /// <param name="mapper">The request mapper.</param>
    /// <param name="timeProvider">The deterministic clock.</param>
    public OverriddenCommandUrlEffect(
        HttpClient httpClient,
        IMapper<CommandUrlAction, CommandUrlRequest> mapper,
        TimeProvider timeProvider
    )
        : base(httpClient, mapper, timeProvider)
    {
    }

    /// <inheritdoc />
    protected override string GetEndpoint(
        CommandUrlAction action
    ) =>
        "/custom/submit";
}