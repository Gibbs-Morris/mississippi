using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;


namespace Mississippi.Reservoir.Core.L0Tests;

/// <summary>
///     Captures the generated listener-failure logging contract for assertions.
/// </summary>
/// <param name="Level">The logged severity.</param>
/// <param name="EventId">The event identifier and name.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The original listener exception.</param>
/// <param name="State">The structured logging fields.</param>
internal sealed record StoreCapturedLog(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    KeyValuePair<string, object?>[] State
);