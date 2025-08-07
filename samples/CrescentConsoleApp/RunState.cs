using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mississippi.CrescentConsoleApp;

internal static class RunStateStore
{
    public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "crescent.state.json");

    public static async Task<RunState> LoadAsync(ILogger logger)
    {
        try
        {
            if (File.Exists(FilePath))
            {
                using FileStream fs = File.OpenRead(FilePath);
                RunState? s = await JsonSerializer.DeserializeAsync<RunState>(fs);
                return s ?? new RunState();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load run state from {Path}", FilePath);
        }
        return new RunState();
    }

    public static async Task SaveAsync(RunState state, ILogger logger)
    {
        try
        {
            using FileStream fs = File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(fs, state, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save run state to {Path}", FilePath);
        }
    }
}

internal sealed class RunState
{
    public string? PrimaryType { get; set; }
    public string? PrimaryId { get; set; }
    public long? PrimaryHead { get; set; }
    public List<StreamState> Streams { get; set; } = new();

    public void UpsertStream(string type, string id, long head)
    {
        StreamState? existing = Streams.FirstOrDefault(s => s.Type == type && s.Id == id);
        if (existing is null)
        {
            Streams.Add(new StreamState { Type = type, Id = id, Head = head });
        }
        else
        {
            existing.Head = head;
        }
    }
}

internal sealed class StreamState
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public long Head { get; set; }
}


