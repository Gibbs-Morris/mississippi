using System;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L2Tests;

/// <summary>
///     Marks an Azurite-backed Blob L2 test.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class AzuriteBlobFactAttribute : FactAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AzuriteBlobFactAttribute" /> class.
    /// </summary>
    public AzuriteBlobFactAttribute()
    {
    }
}
