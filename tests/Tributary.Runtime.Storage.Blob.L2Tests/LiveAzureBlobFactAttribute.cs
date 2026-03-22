using System;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     Marks a live Azure Blob smoke test that should run only when explicit live-cloud configuration is provided.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class LiveAzureBlobFactAttribute : FactAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LiveAzureBlobFactAttribute" /> class.
    /// </summary>
    public LiveAzureBlobFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LiveAzureBlobTestConfiguration.ConnectionStringEnvironmentVariable)))
        {
            Skip =
                $"Set {LiveAzureBlobTestConfiguration.ConnectionStringEnvironmentVariable} to run live Azure Blob smoke tests.";
        }
    }
}
