using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Performs the cached Increment 2 startup validation needed for runnable onboarding.
/// </summary>
internal sealed class ReplicaSinkStartupValidator : IReplicaSinkStartupValidator
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkStartupValidator" /> class.
    /// </summary>
    /// <param name="projectionRegistry">The cached runtime binding registry.</param>
    public ReplicaSinkStartupValidator(
        IReplicaSinkProjectionRegistry projectionRegistry
    )
    {
        ArgumentNullException.ThrowIfNull(projectionRegistry);
        ProjectionRegistry = projectionRegistry;
    }

    private IReplicaSinkProjectionRegistry ProjectionRegistry { get; }

    /// <inheritdoc />
    public async ValueTask ValidateAsync(
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<ReplicaSinkStartupDiagnostic> diagnostics = ProjectionRegistry.GetDiagnostics();
        if (diagnostics.Count > 0)
        {
            throw new InvalidOperationException(ReplicaSinkStartupDiagnostics.FormatValidationFailure(diagnostics));
        }

        foreach (ReplicaSinkBindingDescriptor descriptor in ProjectionRegistry.GetBindingDescriptors())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await descriptor.ProviderHandle.EnsureTargetAsync(descriptor.ValidatedTargetDescriptor, cancellationToken);
        }
    }
}