// <copyright file="UxProjectionsApiPlaceholderTests.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Api.L0Tests;

/// <summary>
///     Unit tests for <see cref="UxProjectionsApiPlaceholder" />.
/// </summary>
[AllureParentSuite("EventSourcing")]
[AllureSuite("UxProjections.Api")]
[AllureSubSuite("Placeholder")]
public sealed class UxProjectionsApiPlaceholderTests
{
    /// <summary>
    ///     Verifies that Status returns the expected ready message.
    /// </summary>
    [Fact(DisplayName = "Status returns ready message")]
    public void StatusReturnsReadyMessage()
    {
        // Arrange & Act
        string result = UxProjectionsApiPlaceholder.Status;

        // Assert
        Assert.Equal("UxProjections API Ready", result);
    }
}