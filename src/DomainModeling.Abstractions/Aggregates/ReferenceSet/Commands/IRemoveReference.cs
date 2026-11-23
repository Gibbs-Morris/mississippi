using System;


namespace Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;

/// <summary>Defines caller-supplied removal inputs for a named collection command.</summary>
/// <typeparam name="TSource">The source projection type.</typeparam>
/// <remarks>Public so application command records can declare their own serializable properties.</remarks>
public interface IRemoveReference<TSource>
{
    /// <summary>Gets the exact source identifier.</summary>
    string SourceId { get; }

    /// <summary>Gets the source projection type associated with this command contract.</summary>
    Type SourceType => typeof(TSource);
}