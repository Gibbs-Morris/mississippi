using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;


namespace Crescent.ConsoleApp.Infrastructure;

/// <summary>
///     Provides persistence helpers for reading and writing the <see cref="RunState" /> used by the console sample.
/// </summary>
internal static class RunStateStore
{
    /// <summary>
    ///     Gets the absolute path of the JSON file where the run state is persisted.
    /// </summary>
    public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "crescent.state.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    ///     Loads the <see cref="RunState" /> from disk if present; otherwise returns a new, empty state.
    /// </summary>
    /// <param name="logger">The logger used to record any non-fatal load issues.</param>
    /// <returns>The loaded or newly created <see cref="RunState" /> instance.</returns>
    public static async Task<RunState> LoadAsync(
        ILogger logger
    )
    {
        try
        {
            if (File.Exists(FilePath))
            {
                using FileStream fs = File.OpenRead(FilePath);
                RunState? state = await JsonSerializer.DeserializeAsync<RunState>(fs, SerializerOptions)
                    .ConfigureAwait(false);
                return state ?? new RunState();
            }
        }
        catch (IOException ioEx)
        {
            logger.FailedToLoadRunState(ioEx, FilePath);
        }
        catch (JsonException jsonEx)
        {
            logger.FailedToParseRunStateJson(jsonEx, FilePath);
        }

        return new();
    }

    /// <summary>
    ///     Persists the provided <see cref="RunState" /> to disk.
    /// </summary>
    /// <param name="state">The state to persist.</param>
    /// <param name="logger">The logger used to record any non-fatal save issues.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public static async Task SaveAsync(
        RunState state,
        ILogger logger
    )
    {
        try
        {
            using FileStream fs = File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fs, state, SerializerOptions).ConfigureAwait(false);
        }
        catch (IOException ioEx)
        {
            logger.FailedToSaveRunState(ioEx, FilePath);
        }
        catch (UnauthorizedAccessException accessEx)
        {
            logger.NoAccessToSaveRunState(accessEx, FilePath);
        }
    }
}