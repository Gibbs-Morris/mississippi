using System;
using System.Collections.Generic;
using System.Threading;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Minimal async-enumerable wrapper that throws a supplied exception when enumeration starts.
/// </summary>
/// <typeparam name="T">The async sequence element type.</typeparam>
internal sealed class ThrowingAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ThrowingAsyncEnumerable{T}" /> class.
    /// </summary>
    /// <param name="exceptionFactory">Factory used to create the exception thrown during enumeration.</param>
    public ThrowingAsyncEnumerable(
        Func<Exception> exceptionFactory
    )
    {
        ExceptionFactory = exceptionFactory ?? throw new ArgumentNullException(nameof(exceptionFactory));
    }

    private Func<Exception> ExceptionFactory { get; }

    /// <inheritdoc />
    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    ) =>
        throw ExceptionFactory();
}