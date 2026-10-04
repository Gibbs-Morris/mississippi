using System.Collections.Generic;
using System.Threading.Tasks;


namespace Mississippi.DomainModeling.Runtime.L0Tests.EventEffects;

/// <summary>
///     Enumerator whose ownership transfers to the dispatcher on acquisition.
/// </summary>
internal sealed class LifecycleEnumerator : IAsyncEnumerator<object>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LifecycleEnumerator" /> class.
    /// </summary>
    /// <param name="effect">The effect recording each lifecycle operation.</param>
    internal LifecycleEnumerator(
        LifecycleEffect effect
    ) =>
        Effect = effect;

    /// <inheritdoc />
    public object Current => Effect.Current;

    private LifecycleEffect Effect { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Effect.DisposeAsync();

    /// <inheritdoc />
    public ValueTask<bool> MoveNextAsync() => Effect.MoveNextAsync();
}