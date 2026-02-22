using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.SignalR;

using Mississippi.Testing.Utilities.SignalR;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="ConnectionRegistry" />.
/// </summary>
public sealed class ConnectionRegistryTests
{
    /// <summary>
    ///     Tests that Count returns zero for empty registry.
    /// </summary>
    [Fact(DisplayName = "Count Returns Zero For Empty Registry")]
    public void CountShouldReturnZeroForEmptyRegistry()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act
        int count = registry.Count;

        // Assert
        Assert.Equal(0, count);
    }

    /// <summary>
    ///     Tests that GetAll returns all connections.
    /// </summary>
    [Fact(DisplayName = "GetAll Returns All Connections")]
    public void GetAllShouldReturnAllConnections()
    {
        // Arrange
        ConnectionRegistry registry = new();
        HubConnectionContext connection1 = HubConnectionContextFactory.Create("conn-1");
        HubConnectionContext connection2 = HubConnectionContextFactory.Create("conn-2");
        registry.TryAdd("conn-1", connection1);
        registry.TryAdd("conn-2", connection2);

        // Act
        IEnumerable<HubConnectionContext> result = registry.GetAll();

        // Assert
        Assert.Contains(connection1, result);
        Assert.Contains(connection2, result);
        Assert.Equal(2, result.Count());
    }

    /// <summary>
    ///     Tests that GetAll returns empty for empty registry.
    /// </summary>
    [Fact(DisplayName = "GetAll Returns Empty For Empty Registry")]
    public void GetAllShouldReturnEmptyForEmptyRegistry()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act
        IEnumerable<HubConnectionContext> result = registry.GetAll();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Tests that GetConnection returns connection when present.
    /// </summary>
    [Fact(DisplayName = "GetConnection Returns Connection When Present")]
    public void GetConnectionShouldReturnConnectionWhenPresent()
    {
        // Arrange
        ConnectionRegistry registry = new();
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        registry.TryAdd("conn-1", connection);

        // Act
        HubConnectionContext? result = registry.GetConnection("conn-1");

        // Assert
        Assert.Same(connection, result);
    }

    /// <summary>
    ///     Tests that GetConnection returns null when not present.
    /// </summary>
    [Fact(DisplayName = "GetConnection Returns Null When Not Present")]
    public void GetConnectionShouldReturnNullWhenNotPresent()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act
        HubConnectionContext? result = registry.GetConnection("nonexistent");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     Tests that GetConnection throws when connectionId is empty.
    /// </summary>
    [Fact(DisplayName = "GetConnection Throws When ConnectionId Is Empty")]
    public void GetConnectionShouldThrowWhenConnectionIdIsEmpty()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.GetConnection(string.Empty));
    }

    /// <summary>
    ///     Tests that GetConnection throws when connectionId is null.
    /// </summary>
    [Fact(DisplayName = "GetConnection Throws When ConnectionId Is Null")]
    public void GetConnectionShouldThrowWhenConnectionIdIsNull()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.GetConnection(null!));
    }

    /// <summary>
    ///     Tests that TryAdd adds connection and increments count.
    /// </summary>
    [Fact(DisplayName = "TryAdd Adds Connection")]
    public void TryAddShouldAddConnection()
    {
        // Arrange
        ConnectionRegistry registry = new();
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");

        // Act
        bool added = registry.TryAdd("conn-1", connection);

        // Assert
        Assert.True(added);
        Assert.Equal(1, registry.Count);
    }

    /// <summary>
    ///     Tests that TryAdd returns false for duplicate connection ID.
    /// </summary>
    [Fact(DisplayName = "TryAdd Returns False For Duplicate")]
    public void TryAddShouldReturnFalseForDuplicate()
    {
        // Arrange
        ConnectionRegistry registry = new();
        HubConnectionContext connection1 = HubConnectionContextFactory.Create("conn-1");
        HubConnectionContext connection2 = HubConnectionContextFactory.Create("conn-1-other");
        registry.TryAdd("conn-1", connection1);

        // Act
        bool added = registry.TryAdd("conn-1", connection2);

        // Assert
        Assert.False(added);
        Assert.Equal(1, registry.Count);
    }

    /// <summary>
    ///     Tests that TryAdd throws when connectionId is null.
    /// </summary>
    [Fact(DisplayName = "TryAdd Throws When ConnectionId Is Null")]
    public void TryAddShouldThrowWhenConnectionIdIsNull()
    {
        // Arrange
        ConnectionRegistry registry = new();
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryAdd(null!, connection));
    }

    /// <summary>
    ///     Tests that TryAdd throws when connection is null.
    /// </summary>
    [Fact(DisplayName = "TryAdd Throws When Connection Is Null")]
    public void TryAddShouldThrowWhenConnectionIsNull()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryAdd("conn-1", null!));
    }

    /// <summary>
    ///     Tests that TryRemove removes connection and decrements count.
    /// </summary>
    [Fact(DisplayName = "TryRemove Removes Connection")]
    public void TryRemoveShouldRemoveConnection()
    {
        // Arrange
        ConnectionRegistry registry = new();
        HubConnectionContext connection = HubConnectionContextFactory.Create("conn-1");
        registry.TryAdd("conn-1", connection);

        // Act
        bool removed = registry.TryRemove("conn-1");

        // Assert
        Assert.True(removed);
        Assert.Equal(0, registry.Count);
    }

    /// <summary>
    ///     Tests that TryRemove returns false when not present.
    /// </summary>
    [Fact(DisplayName = "TryRemove Returns False When Not Present")]
    public void TryRemoveShouldReturnFalseWhenNotPresent()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act
        bool removed = registry.TryRemove("nonexistent");

        // Assert
        Assert.False(removed);
    }

    /// <summary>
    ///     Tests that TryRemove throws when connectionId is null.
    /// </summary>
    [Fact(DisplayName = "TryRemove Throws When ConnectionId Is Null")]
    public void TryRemoveShouldThrowWhenConnectionIdIsNull()
    {
        // Arrange
        ConnectionRegistry registry = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => registry.TryRemove(null!));
    }
}