using System.Collections.Generic;


namespace Mississippi.Common.Abstractions.Mapping;

/// <summary>
///     Defines a generic interface for mapping collections of objects of type <typeparamref name="TFrom" /> to collections
///     of objects of type <typeparamref name="TTo" />.
/// </summary>
/// <typeparam name="TFrom">The type of the source objects.</typeparam>
/// <typeparam name="TTo">The type of the target objects.</typeparam>
public interface IEnumerableMapper<in TFrom, out TTo> : IMapper<IEnumerable<TFrom>, IEnumerable<TTo>>
{
}