using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Domain.Aggregates.BankAccount.Commands;


namespace Spring.Server.Controllers.Aggregates.Mappers;

/// <summary>
///     Maps <see cref="OpenAccountDto" /> to <see cref="OpenAccount" /> command.
/// </summary>
[PendingSourceGenerator]
internal sealed class OpenAccountDtoMapper : IMapper<OpenAccountDto, OpenAccount>
{
    /// <inheritdoc />
    public OpenAccount Map(
        OpenAccountDto input
    ) =>
        new(input.HolderName, input.InitialDeposit);
}