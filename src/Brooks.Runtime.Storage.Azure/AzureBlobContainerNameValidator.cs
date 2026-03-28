using System;


namespace Mississippi.Brooks.Runtime.Storage.Azure;

/// <summary>
///     Validates Azure Blob Storage container names before any network calls are made.
/// </summary>
internal static class AzureBlobContainerNameValidator
{
    /// <summary>
    ///     Validates the supplied container name against Azure Blob Storage naming rules.
    /// </summary>
    /// <param name="containerName">The candidate container name.</param>
    /// <param name="failure">Receives the validation failure reason when validation fails.</param>
    /// <returns><c>true</c> when the name is valid; otherwise, <c>false</c>.</returns>
    internal static bool TryValidate(
        string? containerName,
        out string failure
    )
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            failure = "Container names must not be empty.";
            return false;
        }

        if ((containerName.Length < 3) || (containerName.Length > 63))
        {
            failure = "Container names must be between 3 and 63 characters long.";
            return false;
        }

        if (!char.IsAsciiLetterOrDigit(containerName[0]) || !char.IsAsciiLetterOrDigit(containerName[^1]))
        {
            failure = "Container names must start and end with a letter or digit.";
            return false;
        }

        bool previousWasHyphen = false;
        foreach (char character in containerName)
        {
            bool isLowercaseLetter = (character >= 'a') && (character <= 'z');
            bool isDigit = (character >= '0') && (character <= '9');
            bool isHyphen = character == '-';

            if (!isLowercaseLetter && !isDigit && !isHyphen)
            {
                failure = "Container names may contain only lowercase letters, digits, and single hyphens.";
                return false;
            }

            if (isHyphen && previousWasHyphen)
            {
                failure = "Container names must not contain consecutive hyphens.";
                return false;
            }

            previousWasHyphen = isHyphen;
        }

        failure = string.Empty;
        return true;
    }
}