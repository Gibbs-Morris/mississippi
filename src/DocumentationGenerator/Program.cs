using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator;
using Mississippi.DocumentationGenerator.Analysis;
using Mississippi.DocumentationGenerator.Discovery;
using Mississippi.DocumentationGenerator.Output;
using Mississippi.DocumentationGenerator.Reports;


// Define CLI options
Option<string?> repoRootOption = new("--repoRoot")
{
    Description = "Repository root path. When absent, autodiscovery is used.",
};

Option<string?> docusaurusRootOption = new("--docusaurusRoot")
{
    Description = "Docusaurus root path. When absent, autodiscovery is used.",
};

Option<string?> outputDirOption = new("--outputDir")
{
    Description = "Output directory path. Defaults to {DocusaurusDocsRoot}/generated.",
};

Option<string[]> slnxOption = new("--slnx")
{
    Description = "Solution files to analyze. Defaults to mississippi.slnx and samples.slnx.",
    DefaultValueFactory = _ => Array.Empty<string>(),
};

Option<string[]> reportsOption = new("--reports")
{
    Description = "Reports to run. When absent, all reports run.",
    DefaultValueFactory = _ => Array.Empty<string>(),
};

Option<string?> configOption = new("--config")
{
    Description = "Configuration file path. Defaults to docsgen.json in repo root.",
};

RootCommand rootCommand = new("Mississippi Documentation Generator - Generates documentation from .NET code");
rootCommand.Options.Add(repoRootOption);
rootCommand.Options.Add(docusaurusRootOption);
rootCommand.Options.Add(outputDirOption);
rootCommand.Options.Add(slnxOption);
rootCommand.Options.Add(reportsOption);
rootCommand.Options.Add(configOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    string? repoRoot = parseResult.GetValue(repoRootOption);
    string? docusaurusRoot = parseResult.GetValue(docusaurusRootOption);
    string? outputDir = parseResult.GetValue(outputDirOption);
    string[]? slnxFiles = parseResult.GetValue(slnxOption);
    string[]? reports = parseResult.GetValue(reportsOption);
    string? configPath = parseResult.GetValue(configOption);

    return await RunGeneratorAsync(
        repoRoot,
        docusaurusRoot,
        outputDir,
        slnxFiles?.ToList() ?? new List<string>(),
        reports?.ToList() ?? new List<string>(),
        configPath,
        cancellationToken);
});

return await rootCommand.Parse(args).InvokeAsync();

static async Task<int> RunGeneratorAsync(
    string? repoRootArg,
    string? docusaurusRootArg,
    string? outputDirArg,
    List<string> slnxFilesArg,
    List<string> reportsArg,
    string? configPath,
    CancellationToken cancellationToken
)
{
    try
    {
        // Build host with configuration and DI
        IHost host = CreateHost(repoRootArg, docusaurusRootArg, outputDirArg, slnxFilesArg, reportsArg, configPath);
        ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();
        IOptions<DocumentationGeneratorOptions> options = host.Services.GetRequiredService<IOptions<DocumentationGeneratorOptions>>();

        // Discover paths
        RepositoryDiscovery discovery = new();
        string repoRoot;
        string docusaurusRoot;
        string outputDirectory;
        List<string> solutionFiles;

        try
        {
            repoRoot = !string.IsNullOrEmpty(options.Value.RepoRoot)
                ? options.Value.RepoRoot
                : discovery.DiscoverRepoRoot();

            logger.DiscoveredRepoRoot(repoRoot);

            docusaurusRoot = !string.IsNullOrEmpty(options.Value.DocusaurusRoot)
                ? options.Value.DocusaurusRoot
                : discovery.DiscoverDocusaurusRoot(repoRoot);

            logger.DiscoveredDocusaurusRoot(docusaurusRoot);

            string docusaurusDocsRoot = discovery.DiscoverDocusaurusDocsRoot(docusaurusRoot);
            outputDirectory = !string.IsNullOrEmpty(options.Value.OutputDir)
                ? options.Value.OutputDir
                : Path.Combine(docusaurusDocsRoot, "generated");

            logger.UsingOutputDirectory(outputDirectory);

            solutionFiles = options.Value.SolutionFiles.Count > 0
                ? options.Value.SolutionFiles.Select(f => Path.IsPathRooted(f) ? f : Path.Combine(repoRoot, f)).ToList()
                : RepositoryDiscovery.GetDefaultSolutionFiles(repoRoot).ToList();

            foreach (string solutionFile in solutionFiles)
            {
                if (!File.Exists(solutionFile))
                {
                    throw new FileNotFoundException($"Solution file not found: {solutionFile}");
                }

                logger.UsingSolutionFile(solutionFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Discovery failed: {ex.Message}");
            return 1;
        }

        // Prepare output directory
        DocumentationWriter writer = host.Services.GetRequiredService<DocumentationWriter>();
        writer.PrepareOutputDirectory(outputDirectory);
        logger.OutputDirectoryPrepared(outputDirectory);

        // Create report context
        ReportContext context = new(repoRoot, docusaurusRoot, outputDirectory, solutionFiles);

        // Execute reports
        ReportRegistry registry = host.Services.GetRequiredService<ReportRegistry>();

        List<string> reportsToRun = options.Value.Reports.Count > 0 ? options.Value.Reports : new List<string>();

        await registry.ExecuteAsync(context, reportsToRun, cancellationToken);

        logger.GenerationCompleted(outputDirectory);
        Console.WriteLine($"Documentation generated successfully to: {outputDirectory}");
        return 0;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Operation cancelled.");
        return 130;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Generation failed: {ex.Message}");
        return 1;
    }
}

static IHost CreateHost(
    string? repoRoot,
    string? docusaurusRoot,
    string? outputDir,
    List<string> slnxFiles,
    List<string> reports,
    string? configPath
)
{
    HostBuilder builder = new();

    builder.ConfigureAppConfiguration((context, config) =>
    {
        // Load config file if specified or use default
        string? effectiveConfigPath = configPath;
        if (string.IsNullOrEmpty(effectiveConfigPath))
        {
            // Try to find docsgen.json in repo root via discovery
            try
            {
                RepositoryDiscovery discovery = new();
                string discoveredRepoRoot = !string.IsNullOrEmpty(repoRoot)
                    ? repoRoot
                    : discovery.DiscoverRepoRoot();
                string defaultConfigPath = Path.Combine(discoveredRepoRoot, "docsgen.json");
                if (File.Exists(defaultConfigPath))
                {
                    effectiveConfigPath = defaultConfigPath;
                }
            }
            catch
            {
                // Ignore discovery errors for config
            }
        }

        if (!string.IsNullOrEmpty(effectiveConfigPath) && File.Exists(effectiveConfigPath))
        {
            config.AddJsonFile(effectiveConfigPath, optional: true);
        }
    });

    builder.ConfigureServices((context, services) =>
    {
        // Configure options
        services.Configure<DocumentationGeneratorOptions>(opts =>
        {
            context.Configuration.GetSection("DocumentationGenerator").Bind(opts);

            // Override with CLI arguments
            if (!string.IsNullOrEmpty(repoRoot))
            {
                opts.RepoRoot = repoRoot;
            }

            if (!string.IsNullOrEmpty(docusaurusRoot))
            {
                opts.DocusaurusRoot = docusaurusRoot;
            }

            if (!string.IsNullOrEmpty(outputDir))
            {
                opts.OutputDir = outputDir;
            }

            if (slnxFiles.Count > 0)
            {
                opts.SolutionFiles = slnxFiles;
            }

            if (reports.Count > 0)
            {
                opts.Reports = reports;
            }
        });

        // Register services
        services.AddSingleton<RepositoryDiscovery>();
        services.AddSingleton<ProjectAnalyzer>();
        services.AddSingleton<GrainAnalyzer>();
        services.AddSingleton<DocumentationWriter>();
        services.AddSingleton<ReportRegistry>();

        // Register reports
        services.AddSingleton<IndexReport>();
        services.AddSingleton<DependenciesReport>();
        services.AddSingleton<OrleansGrainReport>();
    });

    builder.ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(opts =>
        {
            opts.SingleLine = true;
            opts.TimestampFormat = "HH:mm:ss ";
        });
        logging.SetMinimumLevel(LogLevel.Information);
    });

    IHost host = builder.Build();

    // Register reports with registry
    ReportRegistry registry = host.Services.GetRequiredService<ReportRegistry>();
    registry.Register(host.Services.GetRequiredService<IndexReport>());
    registry.Register(host.Services.GetRequiredService<DependenciesReport>());
    registry.Register(host.Services.GetRequiredService<OrleansGrainReport>());

    return host;
}

/// <summary>
///     Logger extensions for the Program class.
/// </summary>
public static partial class ProgramLoggerExtensions
{
    /// <summary>
    ///     Logs when the repository root is discovered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The discovered path.</param>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Repository root: {Path}")]
    public static partial void DiscoveredRepoRoot(
        this ILogger logger,
        string path
    );

    /// <summary>
    ///     Logs when the Docusaurus root is discovered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The discovered path.</param>
    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Docusaurus root: {Path}")]
    public static partial void DiscoveredDocusaurusRoot(
        this ILogger logger,
        string path
    );

    /// <summary>
    ///     Logs the output directory being used.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The output directory path.</param>
    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Output directory: {Path}")]
    public static partial void UsingOutputDirectory(
        this ILogger logger,
        string path
    );

    /// <summary>
    ///     Logs a solution file being used.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The solution file path.</param>
    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Using solution: {Path}")]
    public static partial void UsingSolutionFile(
        this ILogger logger,
        string path
    );

    /// <summary>
    ///     Logs when the output directory is prepared.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The output directory path.</param>
    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Debug,
        Message = "Output directory prepared: {Path}")]
    public static partial void OutputDirectoryPrepared(
        this ILogger logger,
        string path
    );

    /// <summary>
    ///     Logs when generation completes.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="path">The output directory path.</param>
    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Information,
        Message = "Documentation generation completed. Output: {Path}")]
    public static partial void GenerationCompleted(
        this ILogger logger,
        string path
    );
}
