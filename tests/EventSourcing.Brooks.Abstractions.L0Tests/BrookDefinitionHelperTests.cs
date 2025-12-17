using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for <see cref="BrookDefinitionHelper" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Brook Definition Helper")]
public class BrookDefinitionHelperTests
{
    /// <summary>
    ///     Test fixture decorated with BrookName attribute.
    /// </summary>
    [BrookName("APP", "MODULE", "STREAM")]
    private sealed class DecoratedType
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DecoratedType" /> class.
        /// </summary>
        private DecoratedType()
        {
        }

        /// <summary>
        ///     Gets the type reference for Type-based API tests.
        /// </summary>
        public static Type AsType { get; } = typeof(DecoratedType);
    }

    /// <summary>
    ///     Test fixture without BrookName attribute.
    /// </summary>
    private sealed class UndecoratedType
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UndecoratedType" /> class.
        /// </summary>
        private UndecoratedType()
        {
        }

        /// <summary>
        ///     Gets the type reference for Type-based API tests.
        /// </summary>
        public static Type AsType { get; } = typeof(UndecoratedType);
    }

    /// <summary>
    ///     GetBrookName should return the brook name from the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameReturnsAttributeValue()
    {
        string brookName = BrookDefinitionHelper.GetBrookName<DecoratedType>();
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     GetBrookName should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BrookDefinitionHelper.GetBrookName<UndecoratedType>());
    }

    /// <summary>
    ///     GetBrookName with Type parameter should return the brook name from the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameWithTypeReturnsAttributeValue()
    {
        string brookName = BrookDefinitionHelper.GetBrookName(DecoratedType.AsType);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     GetBrookName with Type parameter should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameWithTypeThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BrookDefinitionHelper.GetBrookName(UndecoratedType.AsType));
    }

    /// <summary>
    ///     GetBrookName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void GetBrookNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => BrookDefinitionHelper.GetBrookName(null!));
    }

    /// <summary>
    ///     TryGetBrookName should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetBrookNameReturnsFalseWhenAttributeMissing()
    {
        bool result = BrookDefinitionHelper.TryGetBrookName<UndecoratedType>(out string? brookName);
        Assert.False(result);
        Assert.Null(brookName);
    }

    /// <summary>
    ///     TryGetBrookName should return true and the brook name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetBrookNameReturnsTrueWhenAttributeExists()
    {
        bool result = BrookDefinitionHelper.TryGetBrookName<DecoratedType>(out string? brookName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     TryGetBrookName with Type parameter should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetBrookNameWithTypeReturnsFalseWhenAttributeMissing()
    {
        bool result = BrookDefinitionHelper.TryGetBrookName(UndecoratedType.AsType, out string? brookName);
        Assert.False(result);
        Assert.Null(brookName);
    }

    /// <summary>
    ///     TryGetBrookName with Type parameter should return true and the brook name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetBrookNameWithTypeReturnsTrueWhenAttributeExists()
    {
        bool result = BrookDefinitionHelper.TryGetBrookName(DecoratedType.AsType, out string? brookName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     TryGetBrookName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void TryGetBrookNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => BrookDefinitionHelper.TryGetBrookName(null!, out string? _));
    }
}