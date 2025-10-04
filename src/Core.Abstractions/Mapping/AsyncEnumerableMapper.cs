using System.Diagnostics.CodeAnalysis;


namespace Mississippi.Core.Abstractions.Mapping;

/// <summary>
///     Provides an implementation of <see cref="IAsyncEnumerableMapper{TFrom, TTo}" /> that maps asynchronous collections
///     of objects.
/// </summary>
/// <typeparam name="TFrom">The type of the source objects.</typeparam>
/// <typeparam name="TTo">The type of the target objects.</typeparam>
public sealed class AsyncEnumerableMapper<TFrom, TTo> : IAsyncEnumerableMapper<TFrom, TTo>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncEnumerableMapper{TFrom, TTo}" /> class.
    /// </summary>
    /// <param name="mapper">The mapper used to map individual objects.</param>
    public AsyncEnumerableMapper(
        IMapper<TFrom, TTo> mapper
    ) =>
        Mapper = mapper;

    /// <summary>
    ///     Gets the mapper used to map individual objects.
    /// </summary>
    private IMapper<TFrom, TTo> Mapper { get; }

    /// <summary>
    ///     Maps an asynchronous collection of objects of type <typeparamref name="TFrom" /> to an asynchronous collection of
    ///     objects of type <typeparamref name="TTo" />.
    /// </summary>
    /// <param name="input">The source asynchronous collection to map from.</param>
    /// <returns>The mapped asynchronous collection of objects of type <typeparamref name="TTo" />.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4456:Parameter validation in yielding methods should be wrapped",
        Justification = "Required for IAsyncEnumerable.")]
    public async IAsyncEnumerable<TTo> Map(
        IAsyncEnumerable<TFrom> input
    )
    {
        ArgumentNullException.ThrowIfNull(input);
        await foreach (TFrom item in input)
        {
            yield return Mapper.Map(item);
        }
    }
}
