using System;

using Azure;


namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Tests for consumer-facing Azure diagnostics redaction behavior.
/// </summary>
public sealed class AzureDiagnosticsRedactionTests
{
    /// <summary>
    ///     Verifies status-only descriptions are returned when Azure does not provide an error code.
    /// </summary>
    [Fact]
    public void DescribeReturnsStatusOnlyWhenErrorCodeMissing()
    {
        RequestFailedException exception = new(500, "leak https://account.blob.core.windows.net/brooks?sig=secret");

        string description = AzureDiagnosticsRedaction.Describe(exception);

        Assert.Equal("status 500", description);
    }

    /// <summary>
    ///     Verifies permission failures are translated into actionable, sanitized guidance.
    /// </summary>
    [Fact]
    public void CreateContainerAccessExceptionSanitizesAuthorizationFailures()
    {
        RequestFailedException exception = new(403, "leak https://account.blob.core.windows.net/brooks?sig=secret");

        InvalidOperationException translated = AzureDiagnosticsRedaction.CreateContainerAccessException(
            "brooks",
            "brooks-prod",
            "shared-account",
            BrookStorageInitializationMode.ValidateOnly,
            exception);

        Assert.Contains("brooks-prod", translated.Message, System.StringComparison.Ordinal);
        Assert.Contains("shared-account", translated.Message, System.StringComparison.Ordinal);
        Assert.Contains("required Azure Blob Storage permissions", translated.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("https://account.blob.core.windows.net", translated.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("sig=secret", translated.Message, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies not-found failures guide operators toward the required container action.
    /// </summary>
    [Fact]
    public void CreateContainerAccessExceptionGuidesMissingContainerRecovery()
    {
        RequestFailedException exception = new(404, "container missing");

        InvalidOperationException translated = AzureDiagnosticsRedaction.CreateContainerAccessException(
            "locks",
            "locks-prod",
            "shared-account",
            BrookStorageInitializationMode.ValidateOnly,
            exception);

        Assert.Contains("locks-prod", translated.Message, System.StringComparison.Ordinal);
        Assert.Contains("Create the container or switch InitializationMode to ValidateOrCreate", translated.Message, System.StringComparison.Ordinal);
    }
}