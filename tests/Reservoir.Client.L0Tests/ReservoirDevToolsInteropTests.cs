using System;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for ReservoirDevToolsInterop.
/// </summary>
public sealed class ReservoirDevToolsInteropTests
{
    /// <summary>
    ///     Constructor should throw when jsRuntime is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenJsRuntimeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReservoirDevToolsInterop(null!));
    }
}