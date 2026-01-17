using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Messages;


namespace Mississippi.Aqueduct.Abstractions.L0Tests.Messages;

/// <summary>
///     Tests for <see cref="ServerMessage" />.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Server Message")]
public sealed class ServerMessageTests
{
    /// <summary>
    ///     Verifies that all properties can be set together.
    /// </summary>
    [Fact(DisplayName = "All Properties Can Be Set Together")]
    public void AllPropertiesShouldBeSettableTogether()
    {
        // Arrange
        object?[] args = ["data"];

        // Act
        ServerMessage message = new()
        {
            ConnectionId = "conn123",
            MethodName = "Notify",
            Args = args,
        };

        // Assert
        Assert.Equal("conn123", message.ConnectionId);
        Assert.Equal("Notify", message.MethodName);
        Assert.Single(message.Args);
    }

    /// <summary>
    ///     Verifies that Args can be set.
    /// </summary>
    [Fact(DisplayName = "Args Can Be Set")]
    public void ArgsShouldBeSettable()
    {
        // Arrange
        object?[] args = ["hello", 42, null];

        // Act
        ServerMessage message = new()
        {
            Args = args,
        };

        // Assert
        Assert.Equal(3, message.Args.Count);
        Assert.Equal("hello", message.Args[0]);
        Assert.Equal(42, message.Args[1]);
        Assert.Null(message.Args[2]);
    }

    /// <summary>
    ///     Verifies that ConnectionId can be set.
    /// </summary>
    [Fact(DisplayName = "ConnectionId Can Be Set")]
    public void ConnectionIdShouldBeSettable()
    {
        // Act
        ServerMessage message = new()
        {
            ConnectionId = "conn123",
        };

        // Assert
        Assert.Equal("conn123", message.ConnectionId);
    }

    /// <summary>
    ///     Verifies that default values are set correctly.
    /// </summary>
    [Fact(DisplayName = "Default Values Are Set Correctly")]
    public void DefaultValuesShouldBeSetCorrectly()
    {
        // Act
        ServerMessage message = new();

        // Assert
        Assert.Equal(string.Empty, message.ConnectionId);
        Assert.Equal(string.Empty, message.MethodName);
        Assert.Empty(message.Args);
    }

    /// <summary>
    ///     Verifies record equality works correctly.
    /// </summary>
    [Fact(DisplayName = "Equality Works For Equal Messages")]
    public void EqualityShouldWorkForEqualMessages()
    {
        // Arrange
        ServerMessage message1 = new()
        {
            ConnectionId = "conn1",
            MethodName = "Test",
        };
        ServerMessage message2 = new()
        {
            ConnectionId = "conn1",
            MethodName = "Test",
        };

        // Assert
        Assert.Equal(message1, message2);
    }

    /// <summary>
    ///     Verifies record inequality works correctly.
    /// </summary>
    [Fact(DisplayName = "Inequality Works For Different Messages")]
    public void InequalityShouldWorkForDifferentMessages()
    {
        // Arrange
        ServerMessage message1 = new()
        {
            ConnectionId = "conn1",
        };
        ServerMessage message2 = new()
        {
            ConnectionId = "conn2",
        };

        // Assert
        Assert.NotEqual(message1, message2);
    }

    /// <summary>
    ///     Verifies that MethodName can be set.
    /// </summary>
    [Fact(DisplayName = "MethodName Can Be Set")]
    public void MethodNameShouldBeSettable()
    {
        // Act
        ServerMessage message = new()
        {
            MethodName = "ReceiveMessage",
        };

        // Assert
        Assert.Equal("ReceiveMessage", message.MethodName);
    }

    /// <summary>
    ///     Verifies with expression creates modified copy.
    /// </summary>
    [Fact(DisplayName = "With Expression Creates Modified Copy")]
    public void WithExpressionShouldCreateModifiedCopy()
    {
        // Arrange
        ServerMessage original = new()
        {
            ConnectionId = "conn1",
            MethodName = "Original",
        };

        // Act
        ServerMessage modified = original with
        {
            MethodName = "Modified",
        };

        // Assert
        Assert.Equal("Original", original.MethodName);
        Assert.Equal("Modified", modified.MethodName);
        Assert.Equal("conn1", modified.ConnectionId);
    }
}