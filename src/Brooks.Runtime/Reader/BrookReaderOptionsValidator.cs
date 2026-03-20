using Microsoft.Extensions.Options;


namespace Mississippi.Brooks.Runtime.Reader;

/// <summary>
///     Validates <see cref="BrookReaderOptions" /> at startup via <c>ValidateOnStart</c>.
/// </summary>
internal sealed class BrookReaderOptionsValidator : IValidateOptions<BrookReaderOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(
        string? name,
        BrookReaderOptions options
    )
    {
        if (options.BrookSliceSize <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"BrookReaderOptions.BrookSliceSize must be greater than zero but was {options.BrookSliceSize}.");
        }

        return ValidateOptionsResult.Success;
    }
}