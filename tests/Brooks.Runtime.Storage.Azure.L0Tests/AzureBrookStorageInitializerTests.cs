using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Deterministic startup validation tests for Brooks Azure initialization modes.
/// </summary>
public sealed class AzureBrookStorageInitializerTests
{
    /// <summary>
    ///     Verifies ValidateOnly succeeds when both required containers already exist.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncValidateOnlySucceedsWhenContainersExist()
    {
        using AzureInitializerTestContext context = new(
            static request => AzureInitializerTestContext.CreateResponse(HttpStatusCode.OK),
            options =>
            {
                options.InitializationMode = BrookStorageInitializationMode.ValidateOnly;
                options.BlobServiceClientServiceKey = "shared-account";
                options.ContainerName = "brooks-prod";
                options.LockContainerName = "locks-prod";
            });

        await context.Initializer.StartAsync(CancellationToken.None);

        Assert.Equal(
            [
                "GET /brooks-prod?restype=container",
                "GET /brooks-prod?restype=container",
                "GET /locks-prod?restype=container",
                "GET /locks-prod?restype=container",
            ],
            context.Handler.Requests);
    }

    /// <summary>
    ///     Verifies ValidateOrCreate issues create requests before validating container properties.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncValidateOrCreateCreatesContainersBeforeValidation()
    {
        using AzureInitializerTestContext context = new(
            static request => request.Method == HttpMethod.Put
                ? AzureInitializerTestContext.CreateResponse(HttpStatusCode.Created)
                : AzureInitializerTestContext.CreateResponse(HttpStatusCode.OK),
            options =>
            {
                options.InitializationMode = BrookStorageInitializationMode.ValidateOrCreate;
                options.BlobServiceClientServiceKey = "shared-account";
                options.ContainerName = "brooks-prod";
                options.LockContainerName = "locks-prod";
            });

        await context.Initializer.StartAsync(CancellationToken.None);

        Assert.Equal(
            [
                "PUT /brooks-prod?restype=container",
                "GET /brooks-prod?restype=container",
                "PUT /locks-prod?restype=container",
                "GET /locks-prod?restype=container",
            ],
            context.Handler.Requests);
    }

    /// <summary>
    ///     Verifies ValidateOnly fails fast with actionable guidance when a required container is missing.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncValidateOnlyFailsWhenContainerIsMissing()
    {
        using AzureInitializerTestContext context = new(
            static request => request.RequestUri?.AbsolutePath.EndsWith("/brooks-prod", System.StringComparison.Ordinal) == true
                ? AzureInitializerTestContext.CreateResponse(HttpStatusCode.NotFound)
                : AzureInitializerTestContext.CreateResponse(HttpStatusCode.OK),
            options =>
            {
                options.InitializationMode = BrookStorageInitializationMode.ValidateOnly;
                options.BlobServiceClientServiceKey = "shared-account";
                options.ContainerName = "brooks-prod";
                options.LockContainerName = "locks-prod";
            });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Initializer.StartAsync(CancellationToken.None));

        Assert.Contains("brooks", exception.Message, System.StringComparison.Ordinal);
        Assert.Contains("ValidateOnly", exception.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("https://testaccount.blob.core.windows.net", exception.Message, System.StringComparison.Ordinal);
        Assert.Equal(["GET /brooks-prod?restype=container"], context.Handler.Requests);
    }

    /// <summary>
    ///     Verifies Azure SDK failures are translated into sanitized consumer-facing initialization errors.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task StartAsyncSanitizesAzurePermissionFailures()
    {
        using AzureInitializerTestContext context = new(
            static request => AzureInitializerTestContext.CreateResponse(HttpStatusCode.Forbidden),
            options =>
            {
                options.InitializationMode = BrookStorageInitializationMode.ValidateOrCreate;
                options.BlobServiceClientServiceKey = "shared-account";
                options.ContainerName = "brooks-prod";
                options.LockContainerName = "locks-prod";
            });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Initializer.StartAsync(CancellationToken.None));

        Assert.Contains("required Azure Blob Storage permissions", exception.Message, System.StringComparison.Ordinal);
        Assert.Contains("shared-account", exception.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("https://testaccount.blob.core.windows.net", exception.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("sig=secret", exception.Message, System.StringComparison.Ordinal);
        Assert.Equal(["PUT /brooks-prod?restype=container"], context.Handler.Requests);
    }
}