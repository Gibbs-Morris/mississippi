namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;

/// <summary>
///     Represents the execution status of a command.
/// </summary>
public enum CommandStatus
{
    /// <summary>
    ///     The command is currently executing.
    /// </summary>
    Executing,

    /// <summary>
    ///     The command completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    ///     The command failed with an error.
    /// </summary>
    Failed,
}