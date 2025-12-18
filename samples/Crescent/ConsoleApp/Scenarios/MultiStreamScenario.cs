using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Crescent.ConsoleApp.Infrastructure;
using Crescent.ConsoleApp.Shared;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Factory;
using Mississippi.EventSourcing.Reader;
using Mississippi.EventSourcing.Writer;

using Orleans.Runtime;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Runs a multi-stream interleaved workload and reports cursors and counts.
/// </summary>
internal static class MultiStreamScenario
{
    private const string ScenarioName = "MultiStream";

    /// <summary>
    ///     Extracts stream states from a successful scenario result.
    /// </summary>
    /// <param name="result">The scenario result.</param>
    /// <returns>The list of stream states, or an empty list if not found.</returns>
    public static IReadOnlyList<StreamState> GetStreamStates(
        ScenarioResult result
    )
    {
        if ((result.Data != null) &&
            result.Data.TryGetValue("StreamStates", out object? value) &&
            value is List<StreamState> states)
        {
            return states;
        }

        return [];
    }

    /// <summary>
    ///     Executes the multi-stream scenario and returns the result with stream states.
    /// </summary>
    /// <param name="logger">Logger for structured messages.</param>
    /// <param name="runId">Correlation identifier for the current run.</param>
    /// <param name="brookGrainFactory">Factory for resolving Orleans brook grains.</param>
    /// <returns>A scenario result with stream states in the Data property.</returns>
    public static async Task<ScenarioResult> RunAsync(
        ILogger logger,
        string runId,
        IBrookGrainFactory brookGrainFactory
    )
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            BrookKey keyA = new($"test-brook-{Guid.NewGuid():N}", "A");
            BrookKey keyB = new($"test-brook-{Guid.NewGuid():N}", "B");
            IBrookWriterGrain wA = brookGrainFactory.GetBrookWriterGrain(keyA);
            IBrookWriterGrain wB = brookGrainFactory.GetBrookWriterGrain(keyB);
            await wA.AppendEventsAsync(SampleEventFactory.CreateFixedSizeEvents(50, 1024));
            await wB.AppendEventsAsync(SampleEventFactory.CreateRangeSizeEvents(50, 512, 4096));

            // Use confirmed cursor positions to avoid cached -1 during immediate readback
            BrookPosition cA = await brookGrainFactory.GetBrookCursorGrain(keyA).GetLatestPositionConfirmedAsync();
            BrookPosition cB = await brookGrainFactory.GetBrookCursorGrain(keyB).GetLatestPositionConfirmedAsync();
            logger.CursorsAB(runId, cA.Value, cB.Value);

            // Read a portion from each
            IBrookReaderGrain rA = brookGrainFactory.GetBrookReaderGrain(keyA);
            IBrookReaderGrain rB = brookGrainFactory.GetBrookReaderGrain(keyB);
            int ca = 0, cb = 0;
            if (cA.Value >= 1)
            {
                int attemptsA = 0;
                while (true)
                {
                    try
                    {
                        ca = 0;
                        await foreach (BrookEvent ignoredEvent in rA.ReadEventsAsync(new(1), cA))
                        {
                            ca++;
                        }

                        break;
                    }
                    catch (EnumerationAbortedException ex)
                    {
                        if (attemptsA == 0)
                        {
                            attemptsA++;
                            logger.ReadEnumerationAbortedRetry(runId, ex);

                            // retry once
                        }
                        else
                        {
                            // second failure: swallow to avoid crashing the scenario
                            break;
                        }
                    }
                }
            }
            else
            {
                logger.StreamAEmpty(runId);
            }

            if (cB.Value >= 1)
            {
                int attemptsB = 0;
                while (true)
                {
                    try
                    {
                        cb = 0;
                        await foreach (BrookEvent ignoredEvent in rB.ReadEventsAsync(new(1), cB))
                        {
                            cb++;
                        }

                        break;
                    }
                    catch (EnumerationAbortedException ex)
                    {
                        if (attemptsB == 0)
                        {
                            attemptsB++;
                            logger.ReadEnumerationAbortedRetry(runId, ex);

                            // retry once
                        }
                        else
                        {
                            // second failure: swallow to avoid crashing the scenario
                            break;
                        }
                    }
                }
            }
            else
            {
                logger.StreamBEmpty(runId);
            }

            logger.ReadCountsAB(runId, ca, cb);
            List<StreamState> streamStates =
            [
                new()
                {
                    Type = keyA.Type,
                    Id = keyA.Id,
                    Cursor = cA.Value,
                },
                new()
                {
                    Type = keyB.Type,
                    Id = keyB.Id,
                    Cursor = cB.Value,
                },
            ];
            sw.Stop();
            return ScenarioResult.Success(
                ScenarioName,
                (int)sw.ElapsedMilliseconds,
                $"Multi-stream completed: A={ca} events, B={cb} events",
                new Dictionary<string, object>
                {
                    ["StreamStates"] = streamStates,
                });
        }
        catch (OperationCanceledException ex)
        {
            sw.Stop();
            return ScenarioResult.Failure(
                ScenarioName,
                $"Operation cancelled: {ex.Message}",
                (int)sw.ElapsedMilliseconds);
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            return ScenarioResult.Failure(ScenarioName, ex.Message, (int)sw.ElapsedMilliseconds);
        }
    }
}