using System;

using Microsoft.Extensions.Logging.Abstractions;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Tests explicit store logging constructor validation.
/// </summary>
public sealed class StoreLoggingConstructorTests
{
    /// <summary>
    ///     DI-resolved components cannot use a null logger.
    /// </summary>
    [Fact]
    public void ComponentConstructorRejectsNullLogger()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            using Store store = new([], [], TimeProvider.System, null!);
        });
        Assert.Equal("logger", exception.ParamName);
    }

    /// <summary>
    ///     An explicit logger cannot be null.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullLogger()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            using Store store = new(TimeProvider.System, null!);
        });
        Assert.Equal("logger", exception.ParamName);
    }

    /// <summary>
    ///     Explicit logging does not weaken time-provider validation.
    /// </summary>
    [Fact]
    public void LoggingConstructorRejectsNullTimeProvider()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            using Store store = new(null!, NullLogger<Store>.Instance);
        });
        Assert.Equal("timeProvider", exception.ParamName);
    }
}