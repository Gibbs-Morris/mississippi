#if false // Replaced by source generator: CommandClientActionsGenerator
using Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions;
using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Actions;

/// <summary>
///     Action dispatched to open a new bank account.
/// </summary>
/// <remarks>
///     Derived from domain command: <c>Spring.Domain.Aggregates.BankAccount.Commands.OpenAccount</c>.
/// </remarks>
/// <param name="EntityId">The entity ID to create.</param>
/// <param name="HolderName">The name of the account holder.</param>
/// <param name="InitialDeposit">The initial deposit amount.</param>
[PendingSourceGenerator]
internal sealed record OpenAccountAction(string EntityId, string HolderName, decimal InitialDeposit) : ICommandAction;
#endif