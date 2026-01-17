using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Build.Locator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Discovery;
using Mississippi.DocumentationGenerator.Infrastructure;
using Mississippi.DocumentationGenerator.Reports;


namespace Mississippi.DocumentationGenerator;

/// <summary>
///     Entry point for the Documentation Generator console application.
/// </summary>
public static class Program
{
    /// <summary>
    ///     Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code: 0 for success, non-zero for failure.</returns>
    public static async Task<int> Main(
        string[] args
    )
    {
        try
        {
            // Register MSBuild before anything else
            if (!MSBuildLocator.IsRegistered)
            {
                VisualStudioInstance? instance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
                if (instance != null)
                {
                    MSBuildLocator.RegisterInstance(instance);
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                }
            }

            IHost host = CreateHostBuilder(args).Build();

            ILogger<DocumentationGeneratorService> logger =
                host.Services.GetRequiredService<ILogger<DocumentationGeneratorService>>();
            DocumentationGeneratorService generator =
                host.Services.GetRequiredService<DocumentationGeneratorService>();

            using CancellationTokenSource cts = new();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            int result = await generator.RunAsync(cts.Token);
            return result;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation cancelled.");
            return 1;
        }
        catch (DocGenException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static IHostBuilder CreateHostBuilder(
        string[] args
    )
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Parse command line for config path override
                Dictionary<string, string?> cmdLineConfig = ParseCommandLine(args);
                string configPath = cmdLineConfig.GetValueOrDefault("config") ??
                                    Path.Combine(Environment.CurrentDirectory, "docsgen.json");

                if (File.Exists(configPath))
                {
                    config.AddJsonFile(configPath, optional: true, reloadOnChange: false);
                }

                config.AddCommandLine(args, new Dictionary<string, string>
                {
                    { "--repoRoot", "DocGen:RepoRoot" },
                    { "--docusaurusRoot", "DocGen:DocusaurusRoot" },
                    { "--outputDir", "DocGen:OutputDir" },
                    { "--slnx", "DocGen:Solutions" },
                    { "--reports", "DocGen:Reports" },
                    { "--config", "DocGen:ConfigPath" }
                });
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<DocGenOptions>(context.Configuration.GetSection("DocGen"));
                services.Configure<ClassDiagramOptions>(context.Configuration.GetSection("classDiagrams"));
                services.Configure<ProjectDependencyOptions>(context.Configuration.GetSection("projectDependencies"));
                services.Configure<OrleansOptions>(context.Configuration.GetSection("orleans"));

                services.AddSingleton<PathDiscovery>();
                services.AddSingleton<DeterministicWriter>();

                // Register reports
                services.AddSingleton<IReport, ProjectDependencyReport>();
                services.AddSingleton<IReport, ClassDiagramReport>();
                services.AddSingleton<IReport, OrleansGrainReport>();

                services.AddSingleton<ReportRegistry>();
                services.AddSingleton<DocumentationGeneratorService>();
            });
    }

    private static Dictionary<string, string?> ParseCommandLine(
        string[] args
    )
    {
        Dictionary<string, string?> result = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                string key = args[i][2..];
                result[key] = args[i + 1];
                i++;
            }
        }

        return result;
    }
}
