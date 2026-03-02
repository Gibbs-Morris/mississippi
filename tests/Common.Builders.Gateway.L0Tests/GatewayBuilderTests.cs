using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Gateway.Abstractions;


namespace Mississippi.Common.Builders.Gateway.L0Tests;

/// <summary>
///     L0 tests for <see cref="GatewayBuilder" />.
/// </summary>
public sealed class GatewayBuilderTests
{
    [Fact]
    public void AllowAnonymousExplicitlyShouldEnableAnonymousFlag()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        IGatewayBuilder result = builder.AllowAnonymousExplicitly();
        Assert.True(builder.AnonymousExplicitlyAllowed);
        Assert.Same(builder, result);
    }

    [Fact]
    public void ConfigureAuthorizationShouldEnableAuthorizationFlag()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        IGatewayBuilder result = builder.ConfigureAuthorization();
        Assert.True(builder.AuthorizationConfigured);
        Assert.Same(builder, result);
    }

    [Fact]
    public void EnsureAuthorizationConfiguredShouldNotThrowWhenAnonymousIsExplicitlyAllowed()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        builder.AllowAnonymousExplicitly();
        builder.EnsureAuthorizationConfigured();
    }

    [Fact]
    public void EnsureAuthorizationConfiguredShouldNotThrowWhenAuthorizationConfigured()
    {
        GatewayBuilder builder = GatewayBuilder.Create();
        builder.ConfigureAuthorization();
        builder.EnsureAuthorizationConfigured();
    }

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