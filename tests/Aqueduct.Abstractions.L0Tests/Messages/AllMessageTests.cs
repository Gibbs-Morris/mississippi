using Allure.Xunit.Attributes;

using Mississippi.Aqueduct.Abstractions.Messages;


namespace Mississippi.Aqueduct.Abstractions.L0Tests.Messages;

/// <summary>
///     Tests for <see cref="AllMessage" />.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("All Message")]
public sealed class AllMessageTests
{
    /// <summary>
    ///     Verifies that all properties can be set together.
    /// </summary>
    [Fact(DisplayName = "All Properties Can Be Set Together")]
    public void AllPropertiesShouldBeSettableTogether()
    {
        // Arrange
        object?[] args = ["data"];
        string[] excluded = ["conn1"];

        // Act
        AllMessage message = new()
        {
            MethodName = "Broadcast",
            Args = args,
            ExcludedConnectionIds = excluded,
        };

        // Assert
        Assert.Equal("Broadcast", message.MethodName);
        Assert.Single(message.Args);
        Assert.NotNull(message.ExcludedConnectionIds);
        Assert.Single(message.ExcludedConnectionIds);
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
        AllMessage message = new()
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
    ///     Verifies that default values are set correctly.
    /// </summary>
    [Fact(DisplayName = "Default Values Are Set Correctly")]
    public void DefaultValuesShouldBeSetCorrectly()
    {
        // Act
        AllMessage message = new();

        // Assert
        Assert.Equal(string.Empty, message.MethodName);
        Assert.Empty(message.Args);
        Assert.Null(message.ExcludedConnectionIds);
    }

    /// <summary>
    ///     Verifies record equality works correctly.
    /// </summary>
    [Fact(DisplayName = "Equality Works For Equal Messages")]
    public void EqualityShouldWorkForEqualMessages()
    {
        // Arrange
        AllMessage message1 = new()
        {
            MethodName = "Test",
        };
        AllMessage message2 = new()
        {
            MethodName = "Test",
        };

        // Assert
        Assert.Equal(message1, message2);
    }

    /// <summary>
    ///     Verifies that ExcludedConnectionIds can be set.
    /// </summary>
    [Fact(DisplayName = "ExcludedConnectionIds Can Be Set")]
    public void ExcludedConnectionIdsShouldBeSettable()
    {
        // Arrange
        string[] excluded = ["conn1", "conn2"];

        // Act
        AllMessage message = new()
        {
            ExcludedConnectionIds = excluded,
        };

        // Assert
        Assert.NotNull(message.ExcludedConnectionIds);
        Assert.Equal(2, message.ExcludedConnectionIds.Count);
        Assert.Equal("conn1", message.ExcludedConnectionIds[0]);
        Assert.Equal("conn2", message.ExcludedConnectionIds[1]);
    }

    /// <summary>
    ///     Verifies record inequality works correctly.
    /// </summary>
    [Fact(DisplayName = "Inequality Works For Different Messages")]
    public void InequalityShouldWorkForDifferentMessages()
    {
        // Arrange
        AllMessage message1 = new()
        {
            MethodName = "Test1",
        };
        AllMessage message2 = new()
        {
            MethodName = "Test2",
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
        AllMessage message = new()
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
        AllMessage original = new()
        {
            MethodName = "Original",
        };

        // Act
        AllMessage modified = original with
        {
            MethodName = "Modified",
        };

        // Assert
        Assert.Equal("Original", original.MethodName);
        Assert.Equal("Modified", modified.MethodName);
    }
}