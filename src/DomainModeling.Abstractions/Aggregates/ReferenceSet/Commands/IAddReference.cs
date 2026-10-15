using System;


namespace Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;

/// <summary>Defines caller-supplied membership inputs for a named collection command.</summary>
/// <typeparam name="TSource">The source projection type; no source instance is stored or queried.</typeparam>
/// <remarks>Public so application command records can declare their own serializable properties.</remarks>
public interface IAddReference<TSource>
{
    /// <summary>Gets the nonnegative source version observed when adding membership.</summary>
    long ObservedSourceVersion { get; }

    /// <summary>Gets the exact source identifier.</summary>
    string SourceId { get; }

    /// <summary>Gets the source projection type associated with this command contract.</summary>
    Type SourceType => typeof(TSource);
}