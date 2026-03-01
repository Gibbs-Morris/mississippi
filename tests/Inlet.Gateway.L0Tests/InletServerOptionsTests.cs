using Mississippi.Common.Abstractions;


namespace Mississippi.Inlet.Gateway.L0Tests;

/// <summary>
///     Tests for <see cref="InletServerOptions" />.
/// </summary>
public sealed class InletServerOptionsTests
{
    /// <summary>
    ///     AllClientsStreamNamespace should have correct default value.
    /// </summary>
    [Fact]
    public void AllClientsStreamNamespaceHasCorrectDefault()
    {
        // Arrange
        InletServerOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamNamespaces.AllClients, options.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     AllClientsStreamNamespace should be settable.
    /// </summary>
    [Fact]
    public void AllClientsStreamNamespaceIsSettable()
    {
        // Arrange
        InletServerOptions options = new();

        // Act
        options.AllClientsStreamNamespace = "Custom.Namespace";

        // Assert
        Assert.Equal("Custom.Namespace", options.AllClientsStreamNamespace);
    }

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

    /// <summary>
    ///     HeartbeatIntervalMinutes should have correct default value.
    /// </summary>
    [Fact]
    public void HeartbeatIntervalMinutesHasCorrectDefault()
    {
        // Arrange
        InletServerOptions options = new();

        // Assert
        Assert.Equal(1, options.HeartbeatIntervalMinutes);
    }

    /// <summary>
    ///     HeartbeatIntervalMinutes should be settable.
    /// </summary>
    [Fact]
    public void HeartbeatIntervalMinutesIsSettable()
    {
        // Arrange
        InletServerOptions options = new();

        // Act
        options.HeartbeatIntervalMinutes = 5;

        // Assert
        Assert.Equal(5, options.HeartbeatIntervalMinutes);
    }

    /// <summary>
    ///     ServerStreamNamespace should have correct default value.
    /// </summary>
    [Fact]
    public void ServerStreamNamespaceHasCorrectDefault()
    {
        // Arrange
        InletServerOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamNamespaces.Server, options.ServerStreamNamespace);
    }

    /// <summary>
    ///     ServerStreamNamespace should be settable.
    /// </summary>
    [Fact]
    public void ServerStreamNamespaceIsSettable()
    {
        // Arrange
        InletServerOptions options = new();

        // Act
        options.ServerStreamNamespace = "Custom.Server";

        // Assert
        Assert.Equal("Custom.Server", options.ServerStreamNamespace);
    }

    /// <summary>
    ///     StreamProviderName should have correct default value.
    /// </summary>
    [Fact]
    public void StreamProviderNameHasCorrectDefault()
    {
        // Arrange
        InletServerOptions options = new();

        // Assert
        Assert.Equal(MississippiDefaults.StreamProviderName, options.StreamProviderName);
    }

    /// <summary>
    ///     StreamProviderName should be settable.
    /// </summary>
    [Fact]
    public void StreamProviderNameIsSettable()
    {
        // Arrange
        InletServerOptions options = new();

        // Act
        options.StreamProviderName = "CustomStreams";

        // Assert
        Assert.Equal("CustomStreams", options.StreamProviderName);
    }
}