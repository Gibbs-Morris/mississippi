using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Executes the minimal replica sink onboarding validation during host startup.
/// </summary>
internal sealed class ReplicaSinkStartupValidationService : IHostedService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkStartupValidationService" /> class.
    /// </summary>
    /// <param name="startupValidator">The startup validator.</param>
    public ReplicaSinkStartupValidationService(
        IReplicaSinkStartupValidator startupValidator
    ) =>
        StartupValidator = startupValidator;

    private IReplicaSinkStartupValidator StartupValidator { get; }

    /// <inheritdoc />
    public Task StartAsync(
        CancellationToken cancellationToken
    ) =>
        StartupValidator.ValidateAsync(cancellationToken).AsTask();

    /// <inheritdoc />
    public Task StopAsync(
        CancellationToken cancellationToken
    ) =>
        Task.CompletedTask;
}