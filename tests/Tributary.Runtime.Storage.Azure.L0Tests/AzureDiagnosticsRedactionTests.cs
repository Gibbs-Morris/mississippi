using System;

using Azure;

using Mississippi.Tributary.Runtime.Storage.Azure;

namespace MississippiTests.Tributary.Runtime.Storage.Azure.L0Tests
{
    /// <summary>
    ///     Tests for <see cref="AzureDiagnosticsRedaction" />.
    /// </summary>
    public sealed class AzureDiagnosticsRedactionTests
    {
        /// <summary>
        ///     Sanitized diagnostics keep status metadata while omitting raw Azure request details.
        /// </summary>
        [Fact]
        public void CreateContainerAccessExceptionRedactsRawAzureUrisAndMessages()
        {
            RequestFailedException requestFailedException = new(
                403,
                "Sensitive failure https://testaccount.blob.core.windows.net/snapshots-prod?sig=secret",
                "AuthorizationFailure",
                innerException: null);

            InvalidOperationException exception = AzureDiagnosticsRedaction.CreateContainerAccessException(
                "snapshots-prod",
                "shared-account",
                SnapshotStorageInitializationMode.ValidateOnly,
                requestFailedException);

            Assert.Contains("snapshots-prod", exception.Message, StringComparison.Ordinal);
            Assert.Contains("shared-account", exception.Message, StringComparison.Ordinal);
            Assert.Contains("status 403 (AuthorizationFailure)", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("https://testaccount.blob.core.windows.net", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("sig=secret", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("Sensitive failure", exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        ///     Status descriptions omit empty error codes rather than emitting noisy empty parentheses.
        /// </summary>
        [Fact]
        public void DescribeOmitsTheErrorCodeWhenAzureDoesNotSupplyOne()
        {
            RequestFailedException requestFailedException = new(404, "Not found", errorCode: null, innerException: null);

            string description = AzureDiagnosticsRedaction.Describe(requestFailedException);

            Assert.Equal("status 404", description);
        }
    }
}
