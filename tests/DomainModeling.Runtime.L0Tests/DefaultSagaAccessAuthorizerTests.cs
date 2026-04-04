using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="DefaultSagaAccessAuthorizer" />.
/// </summary>
public sealed class DefaultSagaAccessAuthorizerTests
{
    /// <summary>
    ///     Verifies matching fingerprints authorize the request.
    /// </summary>
    [Fact]
    public void AuthorizeAllowsWhenFingerprintsMatch()
    {
        DefaultSagaAccessAuthorizer authorizer = new();
        SagaAccessAuthorizationResult result = authorizer.Authorize(
            "saga-123",
            SagaAccessAction.ReadRuntimeStatus,
            "tenant:user-a",
            "tenant:user-a");
        Assert.True(result.IsAuthorized);
        Assert.Null(result.FailureReason);
    }

    /// <summary>
    ///     Verifies legacy sagas without a stored fingerprint remain accessible.
    /// </summary>
    [Fact]
    public void AuthorizeAllowsWhenStoredFingerprintMissing()
    {
        DefaultSagaAccessAuthorizer authorizer = new();
        SagaAccessAuthorizationResult result = authorizer.Authorize(
            "saga-123",
            SagaAccessAction.Resume,
            null,
            "caller-a");
        Assert.True(result.IsAuthorized);
        Assert.Null(result.FailureReason);
    }

    /// <summary>
    ///     Verifies mismatched fingerprints deny the request.
    /// </summary>
    [Fact]
    public void AuthorizeDeniesWhenFingerprintsDiffer()
    {
        DefaultSagaAccessAuthorizer authorizer = new();
        SagaAccessAuthorizationResult result = authorizer.Authorize(
            "saga-123",
            SagaAccessAction.ReadState,
            "tenant:user-a",
            "tenant:user-b");
        Assert.False(result.IsAuthorized);
        Assert.Equal("The current caller is not authorized for this saga.", result.FailureReason);
    }
}