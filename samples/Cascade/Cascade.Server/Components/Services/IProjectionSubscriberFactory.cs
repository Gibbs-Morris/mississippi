namespace Cascade.Server.Components.Services;

/// <summary>
///     Factory for creating <see cref="IProjectionSubscriber{T}" /> instances.
/// </summary>
/// <remarks>
///     Use this factory when you need to create multiple subscription instances
///     dynamically, such as when subscribing to multiple entities of the same type.
/// </remarks>
internal interface IProjectionSubscriberFactory
{
    /// <summary>
    ///     Creates a new projection subscriber for the specified projection type.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <returns>A new <see cref="IProjectionSubscriber{T}" /> instance.</returns>
    IProjectionSubscriber<T> Create<T>()
        where T : class;
}