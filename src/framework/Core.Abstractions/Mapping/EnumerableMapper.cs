using System;
using System.Collections.Generic;
using System.Linq;

namespace Mississippi.Core.Abstractions.Mapping;

/// <summary>
///     Provides an implementation of <see cref="IEnumerableMapper{TFrom, TTo}" /> that maps collections of objects.
/// </summary>
/// <typeparam name="TFrom">The type of the source objects.</typeparam>
/// <typeparam name="TTo">The type of the target objects.</typeparam>
public sealed class EnumerableMapper<TFrom, TTo> : IEnumerableMapper<TFrom, TTo>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EnumerableMapper{TFrom, TTo}" /> class.
    /// </summary>
    /// <param name="mapper">The mapper used to map individual objects.</param>
    public EnumerableMapper(
        IMapper<TFrom, TTo> mapper
    ) =>
        Mapper = mapper;

    /// <summary>
    ///     Gets the mapper used to map individual objects.
    /// </summary>
    private IMapper<TFrom, TTo> Mapper { get; }

    /// <summary>
    ///     Maps a collection of objects of type <typeparamref name="TFrom" /> to a collection of objects of type
    ///     <typeparamref name="TTo" />.
    /// </summary>
    /// <param name="input">The input collection to map from.</param>
    /// <returns>The mapped collection of objects of type <typeparamref name="TTo" />.</returns>
    public IEnumerable<TTo> Map(
        IEnumerable<TFrom> input
    )
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Select(item => Mapper.Map(item));
    }
}