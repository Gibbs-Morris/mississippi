using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.Features.DualEntitySelection;


namespace MississippiSamples.Spring.Client.L0Tests.Features.DualEntitySelection;

/// <summary>
///     Emits a follow-up selection action for controlled harness verification.
/// </summary>
internal sealed class SelectionFollowUpEffect : ActionEffectBase<SetEntityAIdAction, DualEntitySelectionState>
{
    /// <inheritdoc />
    public override async IAsyncEnumerable<IAction> HandleAsync(
        SetEntityAIdAction action,
        DualEntitySelectionState currentState,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new SetEntityBIdAction(currentState.AccountAId ?? string.Empty);
    }
}