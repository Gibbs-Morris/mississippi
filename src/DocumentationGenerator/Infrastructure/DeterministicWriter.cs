using System.IO;
using System.Text;


namespace Mississippi.DocumentationGenerator.Infrastructure;

/// <summary>
///     Writes documentation files deterministically with consistent line endings.
/// </summary>
public sealed class DeterministicWriter
{
    /// <summary>
    ///     Writes content to a file with LF line endings.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="content">The content to write.</param>
    public void WriteFile(
        string path,
        string content
    )
    {
        // Ensure parent directory exists
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Normalize line endings to LF
        string normalized = content.Replace("\r\n", "\n").Replace("\r", "\n");

        // Write with UTF-8 without BOM
        File.WriteAllText(path, normalized, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    ///     Safely deletes and recreates the output directory.
    ///     Only deletes the specified directory, never parent directories.
    /// </summary>
    /// <param name="outputDir">The output directory to clear.</param>
    public void ClearOutputDirectory(
        string outputDir
    )
    {
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, recursive: true);
        }

        Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    ///     Sanitizes a string to be used as a Mermaid node ID.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>A sanitized ID string.</returns>
    public static string SanitizeId(
        string input
    )
    {
        StringBuilder sb = new(input.Length);
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }

        return sb.ToString();
    }
}
