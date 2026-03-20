using System;

using Mississippi.Common.Abstractions.Diagnostics;


namespace MississippiTests.Sdk.Runtime.L0Tests.Diagnostics;

/// <summary>
///     Tests for <see cref="MississippiBuilderException" />.
/// </summary>
public sealed class MississippiBuilderExceptionTests
{
    /// <summary>
    ///     Constructor with diagnostic code and message should format message correctly.
    /// </summary>
    [Fact]
    public void DiagnosticCodeConstructorFormatsMessageCorrectly()
    {
        // Arrange & Act
        MississippiBuilderException sut = new("MISS-CORE-001", "Test message");

        // Assert
        Assert.Equal("MISS-CORE-001", sut.DiagnosticCode);
        Assert.Equal("[MISS-CORE-001] Test message", sut.Message);
    }

    /// <summary>
    ///     Constructor with diagnostic code, message, and inner exception should format correctly.
    /// </summary>
    [Fact]
    public void DiagnosticCodeWithInnerExceptionFormatsCorrectly()
    {
        // Arrange
        InvalidOperationException inner = new("inner");

        // Act
        MississippiBuilderException sut = new("MISS-RTM-001", "Test message", inner);

        // Assert
        Assert.Equal("MISS-RTM-001", sut.DiagnosticCode);
        Assert.Equal("[MISS-RTM-001] Test message", sut.Message);
        Assert.Same(inner, sut.InnerException);
    }

    /// <summary>
    ///     Message-and-inner constructor should set empty diagnostic code.
    /// </summary>
    [Fact]
    public void MessageAndInnerConstructorSetsEmptyDiagnosticCode()
    {
        // Arrange
        InvalidOperationException inner = new("inner");

        // Act
        MississippiBuilderException sut = new("some message", inner);

        // Assert
        Assert.Equal(string.Empty, sut.DiagnosticCode);
        Assert.Equal("some message", sut.Message);
        Assert.Same(inner, sut.InnerException);
    }

    /// <summary>
    ///     Message-only constructor should set empty diagnostic code.
    /// </summary>
    [Fact]
    public void MessageOnlyConstructorSetsEmptyDiagnosticCode()
    {
        // Act
        MississippiBuilderException sut = new("some message");

        // Assert
        Assert.Equal(string.Empty, sut.DiagnosticCode);
        Assert.Equal("some message", sut.Message);
    }

    /// <summary>
    ///     Parameterless constructor should set empty diagnostic code.
    /// </summary>
    [Fact]
    public void ParameterlessConstructorSetsEmptyDiagnosticCode()
    {
        // Act
        MississippiBuilderException sut = new();

        // Assert
        Assert.Equal(string.Empty, sut.DiagnosticCode);
    }
}