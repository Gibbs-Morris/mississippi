using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;


namespace Mississippi.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Provides analyzer config options for generator tests.
/// </summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptions options;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestAnalyzerConfigOptionsProvider" /> class.
    /// </summary>
    /// <param name="globalOptions">The global options to expose.</param>
    public TestAnalyzerConfigOptionsProvider(
        IReadOnlyDictionary<string, string>? globalOptions = null
    )
    {
        options = new TestAnalyzerConfigOptions(globalOptions ?? new Dictionary<string, string>());
    }

    /// <inheritdoc />
    public override AnalyzerConfigOptions GlobalOptions => options;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(
        SyntaxTree tree
    ) =>
        options;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(
        AdditionalText textFile
    ) =>
        options;

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> options;

        public TestAnalyzerConfigOptions(
            IReadOnlyDictionary<string, string> options
        )
        {
            this.options = options;
        }

        public override bool TryGetValue(
            string key,
            out string value
        )
        {
            if (options.TryGetValue(key, out string? optionValue))
            {
                value = optionValue;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
