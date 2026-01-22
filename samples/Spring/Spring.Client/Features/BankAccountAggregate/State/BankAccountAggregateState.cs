#if FALSE
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.State;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.State;

/// <summary>
///     Feature state for BankAccount aggregate command execution.
/// </summary>
/// <remarks>
///     <para>
///         This state tracks the status of command execution (loading, success, failure)
///         with per-command history and correlation via <see cref="IAggregateCommandState" />.
///     </para>
///     <para>
///         It does NOT hold read model data - that belongs in the projection feature state.
///     </para>
/// </remarks>
[PendingSourceGenerator]
internal sealed record BankAccountAggregateState : AggregateCommandStateBase, IAggregateCommandState
{
    /// <inheritdoc />
    public static string FeatureKey => "bankAccountAggregate";
}
#endif