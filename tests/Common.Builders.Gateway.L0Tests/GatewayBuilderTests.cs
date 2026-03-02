using System;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Gateway.Abstractions;


namespace Mississippi.Common.Builders.Gateway.L0Tests;

/// <summary>
///     L0 tests for <see cref="GatewayBuilder" />.
/// </summary>
public sealed class GatewayBuilderTests
{
    /// <summary>
    ///     AllowAnonymousExplicitly sets the anonymous flag and returns the same builder.
    /// </summary>
    [Fact]
    public void AllowAnonymousExplicitlyShouldEnableAnonymousFlag()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        IGatewayBuilder result = builder.AllowAnonymousExplicitly();
        Assert.True(builder.AnonymousExplicitlyAllowed);
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ConfigureAuthorization sets the authorization flag and returns the same builder.
    /// </summary>
    [Fact]
    public void ConfigureAuthorizationShouldEnableAuthorizationFlag()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        IGatewayBuilder result = builder.ConfigureAuthorization();
        Assert.True(builder.AuthorizationConfigured);
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     EnsureAuthorizationConfigured does not throw when anonymous access is explicitly enabled.
    /// </summary>
    [Fact]
    public void EnsureAuthorizationConfiguredShouldNotThrowWhenAnonymousIsExplicitlyAllowed()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        builder.AllowAnonymousExplicitly();
        Exception? exception = Record.Exception(builder.EnsureAuthorizationConfigured);
        Assert.Null(exception);
    }

    /// <summary>
    ///     EnsureAuthorizationConfigured does not throw when authorization is configured.
    /// </summary>
    [Fact]
    public void EnsureAuthorizationConfiguredShouldNotThrowWhenAuthorizationConfigured()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        builder.ConfigureAuthorization();
        Exception? exception = Record.Exception(builder.EnsureAuthorizationConfigured);
        Assert.Null(exception);
    }

    /// <summary>
    ///     EnsureAuthorizationConfigured throws with expected diagnostic when neither mode is configured.
    /// </summary>
    [Fact]
    public void EnsureAuthorizationConfiguredShouldThrowWhenNeitherAuthNorAnonymousConfigured()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        BuilderValidationException exception =
            Assert.Throws<BuilderValidationException>(() => builder.EnsureAuthorizationConfigured());
        BuilderDiagnostic diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("Gateway.AuthorizationNotConfigured", diagnostic.ErrorCode);
    }
}