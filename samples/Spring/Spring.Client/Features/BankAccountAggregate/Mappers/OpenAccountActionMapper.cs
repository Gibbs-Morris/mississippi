#if false // Replaced by source generator: CommandClientMappersGenerator
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Sdk.Generators.Abstractions;

using Spring.Client.Features.BankAccountAggregate.Actions;
using Spring.Client.Features.BankAccountAggregate.Dtos;


namespace Spring.Client.Features.BankAccountAggregate.Mappers;

/// <summary>
///     Maps <see cref="OpenAccountAction" /> to <see cref="OpenAccountRequestDto" />.
/// </summary>
[PendingSourceGenerator]
internal sealed class OpenAccountActionMapper : IMapper<OpenAccountAction, OpenAccountRequestDto>
{
    /// <inheritdoc />
    public OpenAccountRequestDto Map(
        OpenAccountAction input
    ) =>
        new(input.HolderName, input.InitialDeposit);
}
#endif