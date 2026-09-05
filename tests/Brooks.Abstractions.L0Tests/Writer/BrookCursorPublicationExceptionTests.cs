using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Abstractions.Writer;

using Orleans.Serialization;


namespace Mississippi.Brooks.Abstractions.L0Tests.Writer;

/// <summary>
///     Verifies committed append evidence survives exception construction and Orleans transport.
/// </summary>
public sealed class BrookCursorPublicationExceptionTests
{
    /// <summary>
    ///     Keeps the committed position and underlying publication error across serialization.
    /// </summary>
    /// <param name="isBaseException">Whether the transport contract is the base exception type.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SerializationPreservesCommittedPositionAndFailure(
        bool isBaseException
    )
    {
        ServiceCollection services = new();
        services.AddSerializer(builder => builder.AddAssembly(typeof(BrookCursorPublicationException).Assembly));
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Serializer serializer = serviceProvider.GetRequiredService<Serializer>();
        BrookCursorPublicationException original = new(
            new BrookPosition(42),
            new InvalidOperationException("Stream unavailable."));
        byte[] payload = isBaseException
            ? serializer.SerializeToArray<Exception>(original)
            : serializer.SerializeToArray(original);
        Exception? deserialized = isBaseException
            ? serializer.Deserialize<Exception>(payload)
            : serializer.Deserialize<BrookCursorPublicationException>(payload);
        BrookCursorPublicationException restored = Assert.IsType<BrookCursorPublicationException>(deserialized);
        Assert.Equal(42, restored.Position.Value);
        Assert.Equal(original.Message, restored.Message);
        InvalidOperationException cause = Assert.IsType<InvalidOperationException>(restored.InnerException);
        Assert.Equal("Stream unavailable.", cause.Message);
    }

    /// <summary>
    ///     Does not fabricate a committed position when one was not supplied.
    /// </summary>
    [Fact]
    public void StandardConstructorsLeavePositionUnset()
    {
        InvalidOperationException cause = new("Stream unavailable.");
        BrookCursorPublicationException empty = new();
        BrookCursorPublicationException withMessage = new("Publication failed.");
        BrookCursorPublicationException withCause = new("Publication failed.", cause);
        Assert.True(empty.Position.NotSet);
        Assert.True(withMessage.Position.NotSet);
        Assert.True(withCause.Position.NotSet);
        Assert.Equal("Publication failed.", withMessage.Message);
        Assert.Equal("Publication failed.", withCause.Message);
        Assert.Same(cause, withCause.InnerException);
    }
}