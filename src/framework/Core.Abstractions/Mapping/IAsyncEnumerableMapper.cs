using System.Collections.Generic;


namespace Grove.Core.Mapping;

/// <summary>
///     Defines a generic interface for mapping asynchronous collections of objects of type <typeparamref name="TFrom" />
///     to asynchronous collections of objects of type <typeparamref name="TTo" />.
/// </summary>
/// <typeparam name="TFrom">The type of the source objects.</typeparam>
/// <typeparam name="TTo">The type of the target objects.</typeparam>
public interface IAsyncEnumerableMapper<in TFrom, out TTo> : IMapper<IAsyncEnumerable<TFrom>, IAsyncEnumerable<TTo>>
{
}