using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.DocumentationGenerator.Output;
using Mississippi.DocumentationGenerator.Reports;


namespace Mississippi.DocumentationGenerator.L0Tests.Reports;

/// <summary>
///     Unit tests for IndexReport.
/// </summary>
[AllureParentSuite("Documentation Generator")]
[AllureSuite("Reports")]
[AllureSubSuite("Index Report")]
public sealed class IndexReportTests : IDisposable
{
    private static readonly string[] TestSolutions = new[] { "test.slnx" };
    private readonly string tempDir;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IndexReportTests" /> class.
    /// </summary>
    public IndexReportTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"IndexReportTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    /// <summary>
    ///     Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    ///     Tests that Name returns correct value.
    /// </summary>
    [Fact(DisplayName = "Name returns correct value")]
    public void NameReturnsCorrectValue()
    {
        // Arrange
        DocumentationWriter writer = new();
        IndexReport report = new(writer);

        // Assert
        Assert.Equal("Index", report.Name);
    }

    /// <summary>
    ///     Tests that Order returns zero.
    /// </summary>
    [Fact(DisplayName = "Order returns zero")]
    public void OrderReturnsZero()
    {
        // Arrange
        DocumentationWriter writer = new();
        IndexReport report = new(writer);

        // Assert
        Assert.Equal(0, report.Order);
    }

    /// <summary>
    ///     Tests that GenerateAsync creates index file.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "GenerateAsync creates index file")]
    public async Task GenerateAsyncCreatesIndexFile()
    {
        // Arrange
        DocumentationWriter writer = new();
        IndexReport report = new(writer);
        ReportContext context = new("/repo", "/docs", tempDir, TestSolutions);

        // Act
        await report.GenerateAsync(context, CancellationToken.None);

        // Assert
        string indexPath = Path.Combine(tempDir, "index.mdx");
        Assert.True(File.Exists(indexPath));
    }

    /// <summary>
    ///     Tests that GenerateAsync creates index with correct title.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "GenerateAsync creates index with correct title")]
    public async Task GenerateAsyncCreatesIndexWithCorrectTitle()
    {
        // Arrange
        DocumentationWriter writer = new();
        IndexReport report = new(writer);
        ReportContext context = new("/repo", "/docs", tempDir, TestSolutions);

        // Act
        await report.GenerateAsync(context, CancellationToken.None);

        // Assert
        string indexPath = Path.Combine(tempDir, "index.mdx");
        string content = await File.ReadAllTextAsync(indexPath);
        Assert.Contains("# Generated Documentation", content, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tests that GenerateAsync includes links to other reports.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "GenerateAsync includes links to other reports")]
    public async Task GenerateAsyncIncludesLinksToOtherReports()
    {
        // Arrange
        DocumentationWriter writer = new();
        IndexReport report = new(writer);
        ReportContext context = new("/repo", "/docs", tempDir, TestSolutions);

        // Act
        await report.GenerateAsync(context, CancellationToken.None);

        // Assert
        string indexPath = Path.Combine(tempDir, "index.mdx");
        string content = await File.ReadAllTextAsync(indexPath);
        Assert.Contains("Dependencies", content, StringComparison.Ordinal);
        Assert.Contains("Orleans Grains", content, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tests that GenerateAsync throws for null context.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "GenerateAsync throws for null context")]
    public async Task GenerateAsyncThrowsForNullContext()
    {
        // Arrange
        DocumentationWriter writer = new();
        IndexReport report = new(writer);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            report.GenerateAsync(null!, CancellationToken.None));
    }
}
