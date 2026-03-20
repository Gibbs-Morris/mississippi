using Mississippi.Common.Abstractions.Diagnostics;


namespace MississippiTests.Sdk.Runtime.L0Tests.Diagnostics;

/// <summary>
///     Tests for <see cref="MississippiDiagnosticCodes" />.
/// </summary>
public sealed class MississippiDiagnosticCodesTests
{
    /// <summary>
    ///     All diagnostic codes follow MISS-AREA-NNN format.
    /// </summary>
    /// <param name="code">The diagnostic code to verify.</param>
    [Theory]
    [InlineData("MISS-CORE-001")]
    [InlineData("MISS-CORE-002")]
    [InlineData("MISS-CORE-003")]
    [InlineData("MISS-CORE-004")]
    [InlineData("MISS-CORE-005")]
    [InlineData("MISS-CLI-001")]
    [InlineData("MISS-GTW-001")]
    [InlineData("MISS-GTW-002")]
    [InlineData("MISS-RTM-001")]
    [InlineData("MISS-RTM-002")]
    [InlineData("MISS-RTM-003")]
    [InlineData("MISS-RTM-004")]
    public void AllCodesFollowExpectedFormat(
        string code
    )
    {
        Assert.Matches(@"^MISS-[A-Z]{3,4}-\d{3}$", code);
    }

    /// <summary>
    ///     ClientDuplicateAttach should have CLI prefix.
    /// </summary>
    [Fact]
    public void ClientDuplicateAttachShouldHaveCliPrefix()
    {
        Assert.Equal("MISS-CLI-001", MississippiDiagnosticCodes.ClientDuplicateAttach);
    }

    /// <summary>
    ///     DuplicateAttach should have CORE prefix.
    /// </summary>
    [Fact]
    public void DuplicateAttachShouldHaveCorePrefix()
    {
        Assert.Equal("MISS-CORE-001", MississippiDiagnosticCodes.DuplicateAttach);
    }

    /// <summary>
    ///     DuplicateRegistration should have CORE prefix.
    /// </summary>
    [Fact]
    public void DuplicateRegistrationShouldHaveCorePrefix()
    {
        Assert.Equal("MISS-CORE-004", MississippiDiagnosticCodes.DuplicateRegistration);
    }

    /// <summary>
    ///     GatewayAmbiguousSecurity should have GTW prefix.
    /// </summary>
    [Fact]
    public void GatewayAmbiguousSecurityShouldHaveGtwPrefix()
    {
        Assert.Equal("MISS-GTW-001", MississippiDiagnosticCodes.GatewayAmbiguousSecurity);
    }

    /// <summary>
    ///     GatewayMissingDefaultPolicy should have GTW prefix.
    /// </summary>
    [Fact]
    public void GatewayMissingDefaultPolicyShouldHaveGtwPrefix()
    {
        Assert.Equal("MISS-GTW-002", MississippiDiagnosticCodes.GatewayMissingDefaultPolicy);
    }

    /// <summary>
    ///     InvalidRootConfiguration should have CORE prefix.
    /// </summary>
    [Fact]
    public void InvalidRootConfigurationShouldHaveCorePrefix()
    {
        Assert.Equal("MISS-CORE-003", MississippiDiagnosticCodes.InvalidRootConfiguration);
    }

    /// <summary>
    ///     ReducerEventTypeMismatch should have CORE prefix.
    /// </summary>
    [Fact]
    public void ReducerEventTypeMismatchShouldHaveCorePrefix()
    {
        Assert.Equal("MISS-CORE-005", MississippiDiagnosticCodes.ReducerEventTypeMismatch);
    }

    /// <summary>
    ///     RuntimeDuplicateAttach should have RTM prefix.
    /// </summary>
    [Fact]
    public void RuntimeDuplicateAttachShouldHaveRtmPrefix()
    {
        Assert.Equal("MISS-RTM-001", MississippiDiagnosticCodes.RuntimeDuplicateAttach);
    }

    /// <summary>
    ///     RuntimeInvalidBuilderGraph should have RTM prefix.
    /// </summary>
    [Fact]
    public void RuntimeInvalidBuilderGraphShouldHaveRtmPrefix()
    {
        Assert.Equal("MISS-RTM-003", MississippiDiagnosticCodes.RuntimeInvalidBuilderGraph);
    }

    /// <summary>
    ///     RuntimeMissingStoragePrerequisite should have RTM prefix.
    /// </summary>
    [Fact]
    public void RuntimeMissingStoragePrerequisiteShouldHaveRtmPrefix()
    {
        Assert.Equal("MISS-RTM-002", MississippiDiagnosticCodes.RuntimeMissingStoragePrerequisite);
    }

    /// <summary>
    ///     RuntimeStreamProviderMismatch should have RTM prefix.
    /// </summary>
    [Fact]
    public void RuntimeStreamProviderMismatchShouldHaveRtmPrefix()
    {
        Assert.Equal("MISS-RTM-004", MississippiDiagnosticCodes.RuntimeStreamProviderMismatch);
    }

    /// <summary>
    ///     UnsupportedHostShape should have CORE prefix.
    /// </summary>
    [Fact]
    public void UnsupportedHostShapeShouldHaveCorePrefix()
    {
        Assert.Equal("MISS-CORE-002", MississippiDiagnosticCodes.UnsupportedHostShape);
    }
}