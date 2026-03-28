namespace Mississippi.Brooks.Runtime.Storage.Azure.L0Tests;

/// <summary>
///     Direct tests for Azure blob container name validation edge cases.
/// </summary>
public sealed class AzureBlobContainerNameValidatorTests
{
    /// <summary>
    ///     Verifies known-good shared-account container names pass validation.
    /// </summary>
    /// <param name="containerName">The container name to validate.</param>
    [Theory]
    [InlineData("brooks")]
    [InlineData("locks")]
    [InlineData("snapshots")]
    [InlineData("brooks-prod-01")]
    public void TryValidateReturnsTrueForValidContainerNames(
        string containerName
    )
    {
        bool isValid = AzureBlobContainerNameValidator.TryValidate(containerName, out string failure);

        Assert.True(isValid);
        Assert.Equal(string.Empty, failure);
    }

    /// <summary>
    ///     Verifies Azure-invalid container names are rejected with a reason.
    /// </summary>
    /// <param name="containerName">The invalid container name to validate.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("Invalid")]
    [InlineData("invalid--name")]
    [InlineData("-invalid")]
    [InlineData("invalid-")]
    public void TryValidateReturnsFalseForInvalidContainerNames(
        string? containerName
    )
    {
        bool isValid = AzureBlobContainerNameValidator.TryValidate(containerName, out string failure);

        Assert.False(isValid);
        Assert.NotEqual(string.Empty, failure);
    }
}