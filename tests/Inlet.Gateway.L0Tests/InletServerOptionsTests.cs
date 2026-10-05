namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="InletServerOptions" />.
/// </summary>
public sealed class InletServerOptionsTests
{
    /// <summary>
    ///     GeneratedApiAuthorization should default to disabled mode with allow-anonymous opt-out enabled.
    /// </summary>
    [Fact]
    public void GeneratedApiAuthorizationHasExpectedDefaults()
    {
        // Arrange
        InletServerOptions options = new();

        // Assert
        Assert.NotNull(options.GeneratedApiAuthorization);
        Assert.Equal(GeneratedApiAuthorizationMode.Disabled, options.GeneratedApiAuthorization.Mode);
        Assert.True(options.GeneratedApiAuthorization.AllowAnonymousOptOut);
        Assert.Null(options.GeneratedApiAuthorization.DefaultPolicy);
        Assert.Null(options.GeneratedApiAuthorization.DefaultRoles);
        Assert.Null(options.GeneratedApiAuthorization.DefaultAuthenticationSchemes);
    }

    /// <summary>
    ///     GeneratedApiAuthorization should be configurable.
    /// </summary>
    [Fact]
    public void GeneratedApiAuthorizationIsSettable()
    {
        // Arrange
        InletServerOptions options = new();

        // Act
        options.GeneratedApiAuthorization.Mode =
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints;
        options.GeneratedApiAuthorization.DefaultPolicy = "generated-api-policy";
        options.GeneratedApiAuthorization.DefaultRoles = "admin,operator";
        options.GeneratedApiAuthorization.DefaultAuthenticationSchemes = "Bearer";
        options.GeneratedApiAuthorization.AllowAnonymousOptOut = false;

        // Assert
        Assert.Equal(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            options.GeneratedApiAuthorization.Mode);
        Assert.Equal("generated-api-policy", options.GeneratedApiAuthorization.DefaultPolicy);
        Assert.Equal("admin,operator", options.GeneratedApiAuthorization.DefaultRoles);
        Assert.Equal("Bearer", options.GeneratedApiAuthorization.DefaultAuthenticationSchemes);
        Assert.False(options.GeneratedApiAuthorization.AllowAnonymousOptOut);
    }
}