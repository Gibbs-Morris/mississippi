// NOTE: This file has been replaced by source generation.
// The generated version is in: obj/{Configuration}/net10.0/generated/Mississippi.Inlet.Server.Generators/
//     Mississippi.Inlet.Server.Generators.CommandServerDtoGenerator/OpenAccountDtoMapper.g.cs
// Keeping this file commented out for reference during the generator development phase.

#if false // Replaced by generated code
using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Generators.Abstractions;

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
#endif