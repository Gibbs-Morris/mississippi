using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.DocumentationGenerator.Reports;


namespace Mississippi.DocumentationGenerator.L0Tests.Reports;

/// <summary>
///     Unit tests for ReportRegistry.
/// </summary>
[AllureParentSuite("Documentation Generator")]
[AllureSuite("Reports")]
[AllureSubSuite("Report Registry")]
public sealed class ReportRegistryTests
{
    private static readonly string[] TestSolutions = new[] { "test.slnx" };

    /// <summary>
    ///     Tests that Register adds report to registry.
    /// </summary>
    [Fact(DisplayName = "Register adds report to registry")]
    public void RegisterAddsReportToRegistry()
    {
        // Arrange
        ReportRegistry registry = new(NullLogger<ReportRegistry>.Instance);
        TestReport report = new();

        // Act
        registry.Register(report);

        // Assert
        IReadOnlyList<IDocumentationReport> reports = registry.Reports;
        Assert.Single(reports);
        Assert.Equal("TestReport", reports[0].Name);
    }

    /// <summary>
    ///     Tests that Reports returns reports in order.
    /// </summary>
    [Fact(DisplayName = "Reports returns reports in order")]
    public void ReportsReturnsReportsInOrder()
    {
        // Arrange
        ReportRegistry registry = new(NullLogger<ReportRegistry>.Instance);
        registry.Register(new TestReport("C", 30));
        registry.Register(new TestReport("A", 10));
        registry.Register(new TestReport("B", 20));

        // Act
        IReadOnlyList<IDocumentationReport> reports = registry.Reports;

        // Assert
        Assert.Equal(3, reports.Count);
        Assert.Equal("A", reports[0].Name);
        Assert.Equal("B", reports[1].Name);
        Assert.Equal("C", reports[2].Name);
    }

    /// <summary>
    ///     Tests that ExecuteAsync runs all reports.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "ExecuteAsync runs all reports")]
    public async Task ExecuteAsyncRunsAllReports()
    {
        // Arrange
        ReportRegistry registry = new(NullLogger<ReportRegistry>.Instance);
        TestReport report1 = new("Report1", 10);
        TestReport report2 = new("Report2", 20);
        registry.Register(report1);
        registry.Register(report2);

        ReportContext context = new("/repo", "/docs", "/tmp/test", TestSolutions);

        // Act
        await registry.ExecuteAsync(context, cancellationToken: CancellationToken.None);

        // Assert
        Assert.True(report1.WasExecuted);
        Assert.True(report2.WasExecuted);
    }

    /// <summary>
    ///     Tests that ExecuteAsync with filter runs only matching reports.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "ExecuteAsync with filter runs only matching reports")]
    public async Task ExecuteAsyncWithFilterRunsOnlyMatchingReports()
    {
        // Arrange
        ReportRegistry registry = new(NullLogger<ReportRegistry>.Instance);
        TestReport report1 = new("Report1", 10);
        TestReport report2 = new("Report2", 20);
        registry.Register(report1);
        registry.Register(report2);

        ReportContext context = new("/repo", "/docs", "/tmp/test", TestSolutions);
        string[] reportFilter = new[] { "Report1" };

        // Act
        await registry.ExecuteAsync(context, reportFilter, CancellationToken.None);

        // Assert
        Assert.True(report1.WasExecuted);
        Assert.False(report2.WasExecuted);
    }

    /// <summary>
    ///     Tests that ExecuteAsync throws when no reports match filter.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact(DisplayName = "ExecuteAsync throws when no reports match filter")]
    public async Task ExecuteAsyncThrowsWhenNoReportsMatchFilter()
    {
        // Arrange
        ReportRegistry registry = new(NullLogger<ReportRegistry>.Instance);
        registry.Register(new TestReport("Report1", 10));

        ReportContext context = new("/repo", "/docs", "/tmp/test", TestSolutions);
        string[] reportFilter = new[] { "NonExistent" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registry.ExecuteAsync(context, reportFilter, CancellationToken.None));
    }

    private sealed class TestReport : IDocumentationReport
    {
        public TestReport(string name = "TestReport", int order = 0)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; }

        public string Description => "Test description";

        public int Order { get; }

        public bool WasExecuted { get; private set; }

        public Task GenerateAsync(ReportContext context, CancellationToken cancellationToken = default)
        {
            WasExecuted = true;
            return Task.CompletedTask;
        }
    }
}
