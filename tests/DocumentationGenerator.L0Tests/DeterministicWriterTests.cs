using System;
using System.IO;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Mississippi.DocumentationGenerator.Infrastructure;


namespace Mississippi.DocumentationGenerator.L0Tests;

/// <summary>
///     Tests for <see cref="DeterministicWriter" />.
/// </summary>
[AllureParentSuite("Mississippi.DocumentationGenerator")]
[AllureSuite("Infrastructure")]
[AllureSubSuite("DeterministicWriter")]
public sealed class DeterministicWriterTests : IDisposable
{
    private readonly string testDir;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeterministicWriterTests" /> class.
    /// </summary>
    public DeterministicWriterTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"docgen-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
    }

    /// <summary>
    ///     Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }
    }

    /// <summary>
    ///     WriteFile should create parent directories if they don't exist.
    /// </summary>
    [Fact]
    [AllureFeature("File Writing")]
    public void WriteFileCreatesParentDirectories()
    {
        // Arrange
        DeterministicWriter writer = new();
        string filePath = Path.Combine(testDir, "nested", "dir", "file.txt");

        // Act
        writer.WriteFile(filePath, "content");

        // Assert
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be("content");
    }

    /// <summary>
    ///     WriteFile should normalize CRLF to LF.
    /// </summary>
    [Fact]
    [AllureFeature("File Writing")]
    public void WriteFileNormalizesLineEndings()
    {
        // Arrange
        DeterministicWriter writer = new();
        string filePath = Path.Combine(testDir, "lineendings.txt");
        const string input = "line1\r\nline2\rline3\n";

        // Act
        writer.WriteFile(filePath, input);

        // Assert
        byte[] bytes = File.ReadAllBytes(filePath);
        bytes.Should().NotContain((byte)'\r');
    }

    /// <summary>
    ///     WriteFile should produce identical output for identical input.
    /// </summary>
    [Fact]
    [AllureFeature("Determinism")]
    public void WriteFileProducesDeterministicOutput()
    {
        // Arrange
        DeterministicWriter writer = new();
        string file1 = Path.Combine(testDir, "file1.txt");
        string file2 = Path.Combine(testDir, "file2.txt");
        const string content = "# Title\n\nSome content with\nmultiple lines\n";

        // Act
        writer.WriteFile(file1, content);
        writer.WriteFile(file2, content);

        // Assert
        byte[] bytes1 = File.ReadAllBytes(file1);
        byte[] bytes2 = File.ReadAllBytes(file2);
        bytes1.Should().BeEquivalentTo(bytes2);
    }

    /// <summary>
    ///     ClearOutputDirectory should only delete specified directory.
    /// </summary>
    [Fact]
    [AllureFeature("Safety")]
    public void ClearOutputDirectoryOnlyDeletesSpecifiedDirectory()
    {
        // Arrange
        DeterministicWriter writer = new();
        string siblingDir = Path.Combine(testDir, "sibling");
        string targetDir = Path.Combine(testDir, "target");
        string siblingFile = Path.Combine(siblingDir, "keep.txt");

        Directory.CreateDirectory(siblingDir);
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(siblingFile, "preserved");
        File.WriteAllText(Path.Combine(targetDir, "delete.txt"), "gone");

        // Act
        writer.ClearOutputDirectory(targetDir);

        // Assert
        Directory.Exists(targetDir).Should().BeTrue();
        Directory.GetFiles(targetDir).Should().BeEmpty();
        File.Exists(siblingFile).Should().BeTrue("sibling file should be preserved");
    }

    /// <summary>
    ///     SanitizeId should replace invalid characters.
    /// </summary>
    [Theory]
    [InlineData("Simple", "Simple")]
    [InlineData("With.Dots", "With_Dots")]
    [InlineData("With-Dashes", "With_Dashes")]
    [InlineData("With Spaces", "With_Spaces")]
    [InlineData("Mixed.Name-Here", "Mixed_Name_Here")]
    [AllureFeature("ID Sanitization")]
    public void SanitizeIdReplacesInvalidCharacters(
        string input,
        string expected
    )
    {
        // Act
        string result = DeterministicWriter.SanitizeId(input);

        // Assert
        result.Should().Be(expected);
    }
}
