using Mississippi.Common.Abstractions.Mapping;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Mappers;

/// <summary>
///     Maps <see cref="OpenAccountAction" /> to <see cref="OpenAccountRequestDto" />.
/// </summary>
internal sealed class OpenAccountActionMapper : IMapper<OpenAccountAction, OpenAccountRequestDto>
{
    /// <inheritdoc />
    public OpenAccountRequestDto Map(
        OpenAccountAction input
    ) =>
        new(input.HolderName, input.InitialDeposit);
}
