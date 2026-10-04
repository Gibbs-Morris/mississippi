using System;
using System.Collections.Generic;


namespace Mississippi.Hosting.Abstractions.L0Tests;

/// <summary>
///     Verifies stable exception evidence for invalid composition.
/// </summary>
public sealed class BuilderValidationExceptionTests
{
    /// <summary>
    ///     Diagnostic snapshots cannot be changed through the input or exposed collection.
    /// </summary>
    [Fact]
    public void ExceptionCapturesAnImmutableSnapshot()
    {
        BuilderDiagnostic first = new("MSB001", "Invalid host.", "Attach once.");
        BuilderDiagnostic second = new("MSB002", "Consumed builder.", "Configure before attachment.");
        List<BuilderDiagnostic> diagnostics = [first, second];
        BuilderValidationException exception = new(diagnostics);
        diagnostics.Clear();
        Assert.Equal(new[] { first, second }, exception.Diagnostics);
        Assert.Equal(
            "MSB001: Invalid host. Attach once." +
            Environment.NewLine +
            "MSB002: Consumed builder. Configure before attachment.",
            exception.Message);
        IList<BuilderDiagnostic> exposed = Assert.IsType<IList<BuilderDiagnostic>>(exception.Diagnostics, false);
        Assert.Throws<NotSupportedException>(() => exposed.Clear());
    }

    /// <summary>
    ///     Structured construction requires at least one complete failure.
    /// </summary>
    [Fact]
    public void ExceptionRejectsMissingDiagnostics()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BuilderValidationException((IEnumerable<BuilderDiagnostic>)null!));
        Assert.Throws<ArgumentException>(() => new BuilderValidationException([]));
        Assert.Throws<ArgumentException>(() => new BuilderValidationException([null!]));
    }

    /// <summary>
    ///     Standard exception constructors preserve normal .NET exception behavior.
    /// </summary>
    [Fact]
    public void StandardConstructorsPreserveMessageAndInnerException()
    {
        BuilderValidationException defaultException = new();
        Assert.Contains("composition", defaultException.Message, StringComparison.Ordinal);
        Assert.Empty(defaultException.Diagnostics);
        BuilderValidationException messageException = new("Invalid configuration.");
        Assert.Equal("Invalid configuration.", messageException.Message);
        Assert.Empty(messageException.Diagnostics);
        InvalidOperationException cause = new("Underlying failure.");
        BuilderValidationException wrappedException = new("Invalid configuration.", cause);
        Assert.Equal("Invalid configuration.", wrappedException.Message);
        Assert.Same(cause, wrappedException.InnerException);
        Assert.Empty(wrappedException.Diagnostics);
    }
}