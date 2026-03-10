using System;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     Marks an Azurite-backed Blob L2 test that runs only when the local environment opts into Aspire DCP execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class AzuriteBlobFactAttribute : FactAttribute
{
    /// <summary>
    ///     The environment variable that enables Azurite-backed Blob L2 tests.
    /// </summary>
    public const string EnabledEnvironmentVariable = "MISSISSIPPI_TRIBUTARY_BLOB_AZURITE_L2";

    /// <summary>
    ///     Gets a value indicating whether Azurite-backed Blob L2 tests are enabled.
    /// </summary>
    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnabledEnvironmentVariable),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     Initializes a new instance of the <see cref="AzuriteBlobFactAttribute" /> class.
    /// </summary>
    public AzuriteBlobFactAttribute()
    {
        if (!IsEnabled)
        {
            Skip =
                $"Set {EnabledEnvironmentVariable}=true to run Azurite-backed Blob L2 tests in an Aspire-capable environment.";
        }
    }
}
