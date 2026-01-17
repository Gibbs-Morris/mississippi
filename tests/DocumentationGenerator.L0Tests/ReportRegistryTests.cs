using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Reports;


namespace Mississippi.DocumentationGenerator.L0Tests;

/// <summary>
///     Tests for <see cref="ReportRegistry" />.
/// </summary>
[AllureParentSuite("Mississippi.DocumentationGenerator")]
[AllureSuite("Reports")]
[AllureSubSuite("ReportRegistry")]
public sealed class ReportRegistryTests
{
    /// <summary>
    ///     GetReportsToExecute should return all reports when no filter is set.
    /// </summary>
    [Fact]
    [AllureFeature("Report Selection")]
    public void GetReportsToExecuteReturnsAllWhenNoFilter()
    {
        // Arrange
        IReport[] reports = { new FakeReport("A"), new FakeReport("B"), new FakeReport("C") };
        DocGenOptions options = new();
        ReportRegistry registry = new(reports, Options.Create(options), NullLogger<ReportRegistry>.Instance);

        // Act
        IReadOnlyList<IReport> result = registry.GetReportsToExecute();

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().BeEquivalentTo(new[] { "A", "B", "C" });
    }

    /// <summary>
    ///     GetReportsToExecute should filter by CLI reports option.
    /// </summary>
    [Fact]
    [AllureFeature("Report Selection")]
    public void GetReportsToExecuteFiltersByCliReports()
    {
        // Arrange
        IReport[] reports = { new FakeReport("A"), new FakeReport("B"), new FakeReport("C") };
        DocGenOptions options = new() { Reports = new List<string> { "B" } };
        ReportRegistry registry = new(reports, Options.Create(options), NullLogger<ReportRegistry>.Instance);

        // Act
        IReadOnlyList<IReport> result = registry.GetReportsToExecute();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("B");
    }

    /// <summary>
    ///     GetReportsToExecute should filter by enabledReports config.
    /// </summary>
    [Fact]
    [AllureFeature("Report Selection")]
    public void GetReportsToExecuteFiltersByEnabledReportsConfig()
    {
        // Arrange
        IReport[] reports = { new FakeReport("A"), new FakeReport("B"), new FakeReport("C") };
        DocGenOptions options = new() { EnabledReports = new List<string> { "A", "C" } };
        ReportRegistry registry = new(reports, Options.Create(options), NullLogger<ReportRegistry>.Instance);

        // Act
        IReadOnlyList<IReport> result = registry.GetReportsToExecute();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().BeEquivalentTo(new[] { "A", "C" });
    }

    /// <summary>
    ///     GetReportsToExecute should prefer CLI over config.
    /// </summary>
    [Fact]
    [AllureFeature("Report Selection")]
    public void GetReportsToExecutePrefersCliOverConfig()
    {
        // Arrange
        IReport[] reports = { new FakeReport("A"), new FakeReport("B"), new FakeReport("C") };
        DocGenOptions options = new()
        {
            Reports = new List<string> { "C" },
            EnabledReports = new List<string> { "A", "B" }
        };
        ReportRegistry registry = new(reports, Options.Create(options), NullLogger<ReportRegistry>.Instance);

        // Act
        IReadOnlyList<IReport> result = registry.GetReportsToExecute();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("C");
    }

    /// <summary>
    ///     GetAllReportNames should return sorted names.
    /// </summary>
    [Fact]
    [AllureFeature("Report Metadata")]
    public void GetAllReportNamesReturnsSortedNames()
    {
        // Arrange
        IReport[] reports = { new FakeReport("Zebra"), new FakeReport("Alpha"), new FakeReport("Middle") };
        DocGenOptions options = new();
        ReportRegistry registry = new(reports, Options.Create(options), NullLogger<ReportRegistry>.Instance);

        // Act
        IReadOnlyList<string> names = registry.GetAllReportNames();

        // Assert
        names.Should().BeEquivalentTo(new[] { "Alpha", "Middle", "Zebra" });
        names.Should().BeInAscendingOrder();
    }

    private sealed class FakeReport : IReport
    {
        public FakeReport(
            string name
        )
        {
            Name = name;
        }

        public string Name { get; }

        public string Description => $"Fake report {Name}";

        public Task ExecuteAsync(
            ReportContext context,
            CancellationToken cancellationToken
        )
        {
            return Task.CompletedTask;
        }
    }
}
