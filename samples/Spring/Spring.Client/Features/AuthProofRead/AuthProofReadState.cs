using System;

using Mississippi.Reservoir.Abstractions.State;

using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;


namespace MississippiSamples.Spring.Client.Features.AuthProofRead;

/// <summary>Tracks the protected HTTP observation separately from live subscription snapshots.</summary>
internal sealed record AuthProofReadState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "authProofRead";

    /// <summary>Gets the projection returned by this read attempt.</summary>
    public AuthProofProjectionDto? Data { get; init; }

    /// <summary>Gets the entity associated with this observation.</summary>
    public string? EntityId { get; init; }

    /// <summary>Gets the error from this read attempt.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets a value indicating whether this attempt is awaiting its HTTP result.</summary>
    public bool IsLoading { get; init; }

    /// <summary>Gets the local persona associated with this observation.</summary>
    public string? PersonaName { get; init; }

    /// <summary>Gets the unique current read attempt identifier.</summary>
    public Guid? RequestId { get; init; }

    /// <summary>Gets the observed server projection version.</summary>
    public long Version { get; init; } = -1;
}