namespace Grove.Core.Mapping;

/// <summary>
///     Defines a generic interface for mapping objects of type <typeparamref name="TFrom" /> to objects of type
///     <typeparamref name="TTo" />.
/// </summary>
/// <typeparam name="TFrom">The type of the source object.</typeparam>
/// <typeparam name="TTo">The type of the target object.</typeparam>
public interface IMapper<in TFrom, out TTo>
{
    /// <summary>
    ///     Maps an object of type <typeparamref name="TFrom" /> to an object of type <typeparamref name="TTo" />.
    /// </summary>
    /// <param name="input">The source object to map from.</param>
    /// <returns>The mapped object of type <typeparamref name="TTo" />.</returns>
    TTo Map(
        TFrom input
    );
}