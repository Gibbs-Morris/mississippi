// <copyright file="ChatCreatedReducerTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;

using Allure.Xunit.Attributes;
using Allure.Xunit.Attributes.Steps;

using Crescent.NewModel.Chat;
using Crescent.NewModel.Chat.Events;
using Crescent.NewModel.Chat.Reducers;


namespace Crescent.NewModel.L0Tests.Chat.Reducers;

/// <summary>
///     Tests for <see cref="ChatCreatedReducer" />.
/// </summary>
[AllureParentSuite("Crescent")]
[AllureSuite("NewModel")]
[AllureSubSuite("ChatCreatedReducer")]
public sealed class ChatCreatedReducerTests
{
    /// <summary>
    ///     Verifies that reducing a ChatCreated event produces the correct state.
    /// </summary>
    [Fact]
    [AllureStep("Reduce ChatCreated event produces correct state")]
    public void ReduceChatCreatedEventProducesCorrectState()
    {
        // Arrange
        ChatCreatedReducer reducer = new();
        ChatCreated @event = new()
        {
            Name = "General",
        };

        // Act
        ChatState result = reducer.Reduce(null!, @event);

        // Assert
        Assert.True(result.IsCreated);
        Assert.Equal("General", result.Name);
        Assert.Empty(result.Messages);
    }

    /// <summary>
    ///     Verifies that reducing with a null event throws ArgumentNullException.
    /// </summary>
    [Fact]
    [AllureStep("Reduce with null event throws ArgumentNullException")]
    public void ReduceNullEventThrowsArgumentNullException()
    {
        // Arrange
        ChatCreatedReducer reducer = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reducer.Reduce(null!, null!));
    }
}