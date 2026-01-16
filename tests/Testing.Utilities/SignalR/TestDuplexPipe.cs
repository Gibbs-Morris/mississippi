using System.IO.Pipelines;


namespace Mississippi.Testing.Utilities.SignalR;

/// <summary>
///     Simple duplex pipe implementation for testing.
/// </summary>
internal sealed class TestDuplexPipe : IDuplexPipe
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestDuplexPipe" /> class.
    /// </summary>
    /// <param name="input">The input pipe reader.</param>
    /// <param name="output">The output pipe writer.</param>
    public TestDuplexPipe(
        PipeReader input,
        PipeWriter output
    )
    {
        Input = input;
        Output = output;
    }

    /// <inheritdoc />
    public PipeReader Input { get; }

    /// <inheritdoc />
    public PipeWriter Output { get; }
}