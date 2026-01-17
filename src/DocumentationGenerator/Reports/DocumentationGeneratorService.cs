using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Discovery;
using Mississippi.DocumentationGenerator.Infrastructure;


namespace Mississippi.DocumentationGenerator.Reports;

/// <summary>
///     Main service that orchestrates documentation generation.
/// </summary>
public sealed class DocumentationGeneratorService
{
    private ILogger<DocumentationGeneratorService> Logger { get; }

    private PathDiscovery PathDiscovery { get; }

    private ReportRegistry ReportRegistry { get; }

    private DeterministicWriter Writer { get; }

    private DocGenOptions Options { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DocumentationGeneratorService" /> class.
    /// </summary>
    /// <param name="pathDiscovery">Path discovery service.</param>
    /// <param name="reportRegistry">Report registry.</param>
    /// <param name="writer">Deterministic writer.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public DocumentationGeneratorService(
        PathDiscovery pathDiscovery,
        ReportRegistry reportRegistry,
        DeterministicWriter writer,
        IOptions<DocGenOptions> options,
        ILogger<DocumentationGeneratorService> logger
    )
    {
        PathDiscovery = pathDiscovery;
        ReportRegistry = reportRegistry;
        Writer = writer;
        Options = options.Value;
        Logger = logger;
    }

    /// <summary>
    ///     Runs the documentation generation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code: 0 for success, non-zero for failure.</returns>
    public async Task<int> RunAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Discover paths
            string repoRoot = PathDiscovery.GetRepoRoot();
            string docusaurusRoot = PathDiscovery.GetDocusaurusRoot();
            string outputDir = PathDiscovery.GetOutputDir();

            Console.WriteLine($"Repository root: {repoRoot}");
            Console.WriteLine($"Docusaurus root: {docusaurusRoot}");
            Console.WriteLine($"Output directory: {outputDir}");

            // Determine solutions
            List<string> solutions = GetSolutions(repoRoot);
            Console.WriteLine($"Solutions: {string.Join(", ", solutions.Select(Path.GetFileName))}");

            // Get reports to execute
            IReadOnlyList<IReport> reports = ReportRegistry.GetReportsToExecute();
            Console.WriteLine($"Reports to run: {string.Join(", ", reports.Select(r => r.Name))}");
            Console.WriteLine();

            // Clear output directory (safe: only deletes generated/)
            Logger.LogInformation("Clearing output directory: {OutputDir}", outputDir);
            Writer.ClearOutputDirectory(outputDir);

            // Create context
            ReportContext context = new(repoRoot, outputDir, solutions, Writer);

            // Execute reports in order
            foreach (IReport report in reports)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine($"Running report: {report.Name}...");
                Logger.LogInformation("Executing report: {ReportName} - {Description}", report.Name, report.Description);

                await report.ExecuteAsync(context, cancellationToken);

                Console.WriteLine($"  Completed: {report.Name}");
            }

            // Write index page
            WriteIndexPage(outputDir, reports);

            Console.WriteLine();
            Console.WriteLine("Documentation generation completed successfully.");
            return 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DocGenException ex)
        {
            Logger.LogError(ex, "Documentation generation failed: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during documentation generation");
            throw new DocGenException($"Documentation generation failed: {ex.Message}", ex);
        }
    }

    private List<string> GetSolutions(
        string repoRoot
    )
    {
        if (Options.Solutions.Count > 0)
        {
            return Options.Solutions
                .Select(s => Path.IsPathRooted(s) ? s : Path.Combine(repoRoot, s))
                .ToList();
        }

        // Default: mississippi.slnx and samples.slnx
        return new List<string>
        {
            Path.Combine(repoRoot, "mississippi.slnx"),
            Path.Combine(repoRoot, "samples.slnx")
        };
    }

    private void WriteIndexPage(
        string outputDir,
        IReadOnlyList<IReport> reports
    )
    {
        string content = @"---
sidebar_position: 1
---

# Generated Documentation

This documentation is automatically generated from the Mississippi repository source code.

## Available Reports

";

        foreach (IReport report in reports)
        {
            content += $"- **{report.Name}**: {report.Description}\n";
        }

        content += @"
## How This Works

The documentation generator analyzes the repository's .NET code and produces:

- **Project dependency diagrams** showing relationships between projects
- **Class diagrams** derived from source code via Roslyn analysis
- **Orleans grain call mappings** showing data flow between grains

All diagrams are generated as Mermaid blocks embedded in MDX files.

## Regenerating

To regenerate this documentation locally:

```bash
pwsh ./eng/src/agent-scripts/generate-docs.ps1
```

Or run the generator directly:

```bash
dotnet run --project src/DocumentationGenerator -- --repoRoot .
```

## Important Notes

- This folder (`generated/`) is completely overwritten on each generation
- Do not manually edit files in this directory
- Changes to source code will be reflected after regeneration
";

        Writer.WriteFile(Path.Combine(outputDir, "index.mdx"), content);
    }
}
