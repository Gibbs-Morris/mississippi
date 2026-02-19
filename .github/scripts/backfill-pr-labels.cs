// Backfill PR labels by re-evaluating changed files against current labeler.yml.
//
// Strategy:
//   1. Remove ALL old-taxonomy labels from each PR
//   2. Get the PR's changed files
//   3. Evaluate every rule in labeler.yml against those files
//   4. Apply the computed labels fresh via gh CLI
//
// No rename mapping — pure recalculation from globs.
//
// Environment variables: DRY_RUN, MAX_PRS, STATE, REPO
//
// Usage (local with gh CLI authenticated):
//   $env:DRY_RUN="true"; $env:MAX_PRS="10"; $env:STATE="merged"
//   $env:REPO="Gibbs-Morris/mississippi"
//   dotnet run .github/scripts/backfill-pr-labels.cs
//
// Requires: .NET 10+, gh CLI authenticated

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

// ---------------------------------------------------------------------------
// Old-taxonomy labels to strip (all labels from previous labeler.yml)
// ---------------------------------------------------------------------------
var oldLabels = new HashSet<string>
{
    "area:Aqueduct",
    "area:Common",
    "area:EventSourcing",
    "area:Inlet",
    "area:Refraction",
    "area:Reservoir",
    "area:SDK",
    "api",
    "build-config",
    "ci/cd",
    "documentation",
    "generators",
    "runtime:Orleans",
    "sample:Crescent",
    "sample:Spring",
    "storage:Cosmos",
    "tests-only",
    "ui:Blazor",
};

// Labels this script will never touch (semver labels, manual labels, etc.)
var protectedPrefixes = new[] { "semver:" };

// JsonSerializerOptions with reflection resolver (required in .NET 10)
var jsonOptions = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

// ---------------------------------------------------------------------------
// Configuration from environment
// ---------------------------------------------------------------------------
var dryRun = Environment.GetEnvironmentVariable("DRY_RUN")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;
var maxPrs = int.TryParse(Environment.GetEnvironmentVariable("MAX_PRS"), out var mp) ? mp : 50;
var state = Environment.GetEnvironmentVariable("STATE") ?? "merged";
var repo = Environment.GetEnvironmentVariable("REPO") ?? "";

if (string.IsNullOrEmpty(repo))
{
    repo = RunGh("repo view --json nameWithOwner -q .nameWithOwner").Trim();
}

if (string.IsNullOrEmpty(repo))
{
    Console.Error.WriteLine("ERROR: REPO not set and could not detect from gh CLI");
    return 1;
}

// ---------------------------------------------------------------------------
// Load labeler.yml from the default branch (main) via gh API
// ---------------------------------------------------------------------------
Console.WriteLine($"Fetching .github/labeler.yml from main branch of {repo}...");
string labelerYamlContent;

try
{
    labelerYamlContent = RunGh($"api /repos/{repo}/contents/.github/labeler.yml?ref=main -H \"Accept: application/vnd.github.raw\"");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"ERROR: Could not fetch labeler.yml from main: {ex.Message}");
    return 1;
}

var labelerConfig = ParseLabelerYaml(labelerYamlContent.Split('\n'));

Console.WriteLine($"Loaded {labelerConfig.Count} labels from labeler.yml (main branch)");
Console.WriteLine($"Config: repo={repo} state={state} max_prs={maxPrs} dry_run={dryRun}");
Console.WriteLine($"Old labels to strip: {oldLabels.Count}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// Fetch PRs via gh CLI
// ---------------------------------------------------------------------------
var prsJson = RunGh($"pr list --repo {repo} --state {state} --limit {maxPrs} --json number,title,labels,state,mergedAt");
var prs = JsonSerializer.Deserialize<List<PullRequest>>(prsJson, jsonOptions) ?? [];

Console.WriteLine($"Found {prs.Count} PRs to process");
Console.WriteLine();

var processed = 0;
var modified = 0;
var totalAdded = 0;
var totalRemoved = 0;

foreach (var pr in prs)
{
    Console.WriteLine($"--- PR #{pr.Number}: {Truncate(pr.Title, 80)}");

    var currentLabels = pr.Labels.Select(l => l.Name).ToHashSet();
    Console.WriteLine($"    Current labels: {FormatSet(currentLabels)}");

    // Step 1: Identify old labels to remove
    var toRemove = currentLabels
        .Where(oldLabels.Contains)
        .Where(l => !protectedPrefixes.Any(l.StartsWith))
        .ToHashSet();

    // Step 2: Get changed files
    List<string> files;

    try
    {
        files = GetPrFiles(repo, pr.Number, jsonOptions);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    WARN: Could not fetch files: {ex.Message}");
        files = [];
    }

    if (files.Count == 0)
    {
        Console.WriteLine("    Skipping: no files (empty diff or inaccessible)");
        Console.WriteLine();
        processed++;
        continue;
    }

    Console.WriteLine($"    Changed files: {files.Count}");

    // Step 3: Evaluate all globs to compute fresh labels
    var computedLabels = new HashSet<string>();

    foreach (var (labelName, rules) in labelerConfig)
    {
        if (EvaluateLabel(rules, files))
        {
            computedLabels.Add(labelName);
        }
    }

    Console.WriteLine($"    Computed labels: {FormatSet(computedLabels)}");

    // Step 4: Determine net changes
    var labelsAfterRemoval = currentLabels.Except(toRemove).ToHashSet();
    var toAdd = computedLabels.Except(labelsAfterRemoval).ToHashSet();

    // Don't remove old labels that the new globs also produce (shouldn't happen, but safety)
    toRemove.ExceptWith(computedLabels);

    if (toAdd.Count == 0 && toRemove.Count == 0)
    {
        Console.WriteLine("    No changes needed");
        Console.WriteLine();
        processed++;
        continue;
    }

    if (toRemove.Count > 0)
    {
        Console.WriteLine($"    - Remove: {FormatSet(toRemove)}");
    }

    if (toAdd.Count > 0)
    {
        Console.WriteLine($"    + Add: {FormatSet(toAdd)}");
    }

    if (!dryRun)
    {
        try
        {
            if (toRemove.Count > 0)
            {
                var removeArgs = string.Join(" ", toRemove.Order().Select(l => $"--remove-label \"{l}\""));
                RunGh($"pr edit {pr.Number} --repo {repo} {removeArgs}");
            }

            if (toAdd.Count > 0)
            {
                var addArgs = string.Join(" ", toAdd.Order().Select(l => $"--add-label \"{l}\""));
                RunGh($"pr edit {pr.Number} --repo {repo} {addArgs}");
            }

            Console.WriteLine("    Applied!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ERROR: Failed to update labels: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("    (dry-run, no changes applied)");
    }

    processed++;
    modified++;
    totalAdded += toAdd.Count;
    totalRemoved += toRemove.Count;
    Console.WriteLine();
}

// Summary
Console.WriteLine(new string('=', 60));
Console.WriteLine($"Summary: processed={processed} modified={modified} added={totalAdded} removed={totalRemoved}");

if (dryRun)
{
    Console.WriteLine("(DRY RUN - no changes were applied)");
}

Console.WriteLine(new string('=', 60));
return 0;

// ==========================================================================
// gh CLI helpers
// ==========================================================================

static string RunGh(string arguments)
{
    var psi = new ProcessStartInfo("gh", arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start gh CLI");
    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"gh exited with code {process.ExitCode}: {error}");
    }

    return output;
}

static List<string> GetPrFiles(string repo, int prNumber, JsonSerializerOptions jsonOpts)
{
    var files = new List<string>();
    var page = 1;

    while (true)
    {
        var json = RunGh($"api /repos/{repo}/pulls/{prNumber}/files?per_page=100&page={page}");
        var entries = JsonSerializer.Deserialize<List<PrFile>>(json, jsonOpts) ?? [];

        files.AddRange(entries.Select(f => f.Filename));

        if (entries.Count < 100)
        {
            break;
        }

        page++;
    }

    return files;
}

static string Truncate(string value, int maxLength) =>
    value.Length <= maxLength ? value : value[..maxLength] + "...";

static string FormatSet(IEnumerable<string> items)
{
    var sorted = items.Order().ToList();
    return sorted.Count == 0 ? "(none)" : string.Join(", ", sorted);
}

// ==========================================================================
// Glob matching (minimatch-compatible subset used by actions/labeler)
// ==========================================================================

static Regex GlobToRegex(string pattern)
{
    var parts = new System.Text.StringBuilder("^");
    var i = 0;

    while (i < pattern.Length)
    {
        var c = pattern[i];

        switch (c)
        {
            case '*' when i + 1 < pattern.Length && pattern[i + 1] == '*':
                if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                {
                    parts.Append("(?:.+/)?");
                    i += 3;
                }
                else
                {
                    parts.Append(".*");
                    i += 2;
                }

                break;

            case '*':
                parts.Append("[^/]*");
                i++;
                break;

            case '?':
                parts.Append("[^/]");
                i++;
                break;

            case '.' or '\\' or '(' or ')' or '{' or '}' or '+' or '|' or '^' or '$' or '[' or ']':
                parts.Append('\\');
                parts.Append(c);
                i++;
                break;

            default:
                parts.Append(c);
                i++;
                break;
        }
    }

    parts.Append('$');
    return new Regex(parts.ToString(), RegexOptions.Compiled);
}

static bool FileMatchesPattern(string filepath, string pattern)
{
    if (pattern.StartsWith('!'))
    {
        return !FileMatchesPattern(filepath, pattern[1..]);
    }

    return GlobToRegex(pattern).IsMatch(filepath);
}

// ==========================================================================
// Labeler rule evaluation (actions/labeler v6 semantics)
// ==========================================================================

// actions/labeler v6 semantics for negated globs:
//   Positive globs select files; negated globs (starting with !) exclude files.
//   A file "passes" if it matches at least one positive glob AND none of the negated globs.
//
// any-glob-to-any-file: true if ANY file passes the combined positive+negated check.
// all-globs-to-any-file: each pattern is evaluated independently; true if for EVERY pattern,
//   at least one file matches it (negated patterns invert: true if no file matches the base).

static bool EvaluateAnyGlobToAnyFile(List<string> patterns, List<string> files)
{
    var positive = patterns.Where(p => !p.StartsWith('!')).ToList();
    var negated = patterns.Where(p => p.StartsWith('!')).Select(p => p[1..]).ToList();

    // A file passes if it matches any positive glob and is not excluded by any negated glob
    return files.Any(f =>
        positive.Any(p => GlobToRegex(p).IsMatch(f)) &&
        !negated.Any(n => GlobToRegex(n).IsMatch(f)));
}

static bool EvaluateAllGlobsToAnyFile(List<string> patterns, List<string> files)
{
    // For all-globs-to-any-file, each pattern must be satisfied by at least one file.
    // Negated patterns: satisfied if NO file matches the base pattern.
    return patterns.All(p =>
    {
        if (p.StartsWith('!'))
        {
            var basePattern = p[1..];
            return !files.Any(f => GlobToRegex(basePattern).IsMatch(f));
        }

        return files.Any(f => GlobToRegex(p).IsMatch(f));
    });
}

static bool EvaluateLabel(List<LabelRule> ruleList, List<string> files)
{
    if (files.Count == 0)
    {
        return false;
    }

    foreach (var group in ruleList)
    {
        if (group.ChangedFiles is not { Count: > 0 } matchers)
        {
            continue;
        }

        var groupPasses = true;

        foreach (var matcher in matchers)
        {
            if (matcher.AnyGlobToAnyFile is { Count: > 0 } anyGlobs)
            {
                if (!EvaluateAnyGlobToAnyFile(anyGlobs, files))
                {
                    groupPasses = false;
                    break;
                }
            }

            if (matcher.AllGlobsToAnyFile is { Count: > 0 } allGlobs)
            {
                if (!EvaluateAllGlobsToAnyFile(allGlobs, files))
                {
                    groupPasses = false;
                    break;
                }
            }
        }

        if (groupPasses)
        {
            return true;
        }
    }

    return false;
}

// ==========================================================================
// Simple YAML parser for labeler.yml structure
//
// Handles the specific structure used by actions/labeler v6:
//   label-name:
//     - changed-files:
//         - any-glob-to-any-file:
//             - "glob-pattern"
//         - all-globs-to-any-file:
//             - "!negated-glob"
// ==========================================================================

static Dictionary<string, List<LabelRule>> ParseLabelerYaml(string[] lines)
{
    var result = new Dictionary<string, List<LabelRule>>();

    string? currentLabel = null;
    List<LabelRule>? currentRules = null;
    LabelRule? currentRule = null;
    List<FileMatcher>? currentMatchers = null;
    FileMatcher? currentMatcher = null;
    string? currentMatcherType = null;
    List<string>? currentGlobs = null;

    foreach (var rawLine in lines)
    {
        // Skip empty lines and comments
        if (string.IsNullOrWhiteSpace(rawLine) || rawLine.TrimStart().StartsWith('#'))
        {
            continue;
        }

        var indent = rawLine.Length - rawLine.TrimStart().Length;
        var trimmed = rawLine.Trim();

        // Level 0: label name (no indent, ends with colon)
        if (indent == 0 && trimmed.EndsWith(':'))
        {
            FlushMatcher();
            currentLabel = trimmed[..^1].Trim('"', '\'');
            currentRules = [];
            result[currentLabel] = currentRules;
            currentRule = null;
            currentMatchers = null;
            currentMatcher = null;
            currentMatcherType = null;
            currentGlobs = null;
            continue;
        }

        // Level 1: "- changed-files:" (list item starting a rule group)
        if (indent >= 2 && trimmed == "- changed-files:")
        {
            FlushMatcher();
            currentRule = new LabelRule { ChangedFiles = [] };
            currentMatchers = currentRule.ChangedFiles;
            currentRules?.Add(currentRule);
            currentMatcher = null;
            currentMatcherType = null;
            currentGlobs = null;
            continue;
        }

        // Level 2: "- any-glob-to-any-file:" or "- all-globs-to-any-file:"
        if (indent >= 6 && trimmed.StartsWith("- ") && trimmed.EndsWith(':'))
        {
            FlushMatcher();
            var key = trimmed[2..^1].Trim();
            currentMatcher = new FileMatcher();
            currentMatcherType = key;
            currentGlobs = [];
            currentMatchers?.Add(currentMatcher);
            continue;
        }

        // Level 3: glob pattern "- \"pattern\"" or "- 'pattern'"
        if (indent >= 10 && trimmed.StartsWith("- "))
        {
            var value = trimmed[2..].Trim().Trim('"', '\'');
            currentGlobs?.Add(value);
            continue;
        }
    }

    FlushMatcher();
    return result;

    void FlushMatcher()
    {
        if (currentMatcher is null || currentGlobs is null || currentMatcherType is null)
        {
            return;
        }

        switch (currentMatcherType)
        {
            case "any-glob-to-any-file":
                currentMatcher.AnyGlobToAnyFile = [.. currentGlobs];
                break;
            case "all-globs-to-any-file":
                currentMatcher.AllGlobsToAnyFile = [.. currentGlobs];
                break;
        }

        currentGlobs = null;
        currentMatcherType = null;
    }
}

// ==========================================================================
// JSON models for gh CLI output
// ==========================================================================

record PullRequest
{
    [JsonPropertyName("number")]
    public int Number { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("labels")]
    public List<Label> Labels { get; init; } = [];

    [JsonPropertyName("state")]
    public string State { get; init; } = "";

    [JsonPropertyName("mergedAt")]
    public string? MergedAt { get; init; }
}

record Label
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}

record PrFile
{
    [JsonPropertyName("filename")]
    public string Filename { get; init; } = "";
}

// ==========================================================================
// Internal rule models
// ==========================================================================

class LabelRule
{
    public List<FileMatcher>? ChangedFiles { get; set; }
}

class FileMatcher
{
    public List<string>? AnyGlobToAnyFile { get; set; }

    public List<string>? AllGlobsToAnyFile { get; set; }
}
