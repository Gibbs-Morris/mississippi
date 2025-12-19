// <copyright file="ProjectionGenerator.cs" company="Gibbs-Morris LLC">
// Copyright (c) Gibbs-Morris LLC. All rights reserved.
// </copyright>

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;


namespace Mississippi.EventSourcing.Generators;

/// <summary>
///     Source generator for projection-related boilerplate.
///     Generates UxProjectionGrain, SnapshotCacheGrain, and DI registrations
///     from types marked with [Projection].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ProjectionGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(
        IncrementalGeneratorInitializationContext context
    )
    {
        // Generation pipeline (to be implemented):
        // 1. Find types with [Projection] attribute using ForAttributeWithMetadataName
        // 2. Extract projection metadata (brook type, reducers version, etc.)
        // 3. Generate UxProjectionGrain
        // 4. Generate SnapshotCacheGrain
        // 5. Generate DI registration extension

        // Placeholder: register a marker source to verify the generator is wired correctly
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource(
                "ProjectionGenerator.g.cs",
                SourceText.From(
                    """
                    // Mississippi EventSourcing Generators
                    // Projection generator initialized successfully.
                    """,
                    Encoding.UTF8));
        });
    }
}