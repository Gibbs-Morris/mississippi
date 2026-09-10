using System;

using Mississippi.Hosting.Abstractions;


namespace MississippiTests.Hosting.Abstractions.L0Tests;

/// <summary>
///     Verifies diagnostic values exposed to host applications.
/// </summary>
public sealed class BuilderDiagnosticTests
{
    /// <summary>
    ///     Diagnostics preserve their structured, immutable content.
    /// </summary>
    [Fact]
    public void DiagnosticPreservesContentAndValueEquality()
    {
        BuilderDiagnostic diagnostic = new("MSB001", "Invalid host.", "Attach once.");
        Assert.Equal("MSB001", diagnostic.Code);
        Assert.Equal("Invalid host.", diagnostic.Message);
        Assert.Equal("Attach once.", diagnostic.Remediation);
        Assert.Equal(new("MSB001", "Invalid host.", "Attach once."), diagnostic);
    }

    /// <summary>
    ///     Missing diagnostic content is rejected at construction.
    /// </summary>
    /// <param name="value">The invalid diagnostic content.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DiagnosticRejectsMissingContent(
        string? value
    )
    {
        Assert.ThrowsAny<ArgumentException>(() => new BuilderDiagnostic(value!, "Invalid host.", "Attach once."));
        Assert.ThrowsAny<ArgumentException>(() => new BuilderDiagnostic("MSB001", value!, "Attach once."));
        Assert.ThrowsAny<ArgumentException>(() => new BuilderDiagnostic("MSB001", "Invalid host.", value!));
    }
}