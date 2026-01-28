using System;


using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Brooks.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="BrookNameHelper" /> functionality.
/// </summary>
public class BrookNameHelperTests
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
    ///     GetBrookNameFromGrain should return the brook name from the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameFromGrainReturnsAttributeValue()
    {
        string brookName = BrookNameHelper.GetBrookNameFromGrain(DecoratedType.AsType);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     GetBrookNameFromGrain should throw when attribute is missing.
    /// </summary>
    [Fact]
    public void GetBrookNameFromGrainThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BrookNameHelper.GetBrookNameFromGrain(UndecoratedType.AsType));
    }

    /// <summary>
    ///     GetBrookNameFromGrain should throw when type is null.
    /// </summary>
    [Fact]
    public void GetBrookNameFromGrainThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => BrookNameHelper.GetBrookNameFromGrain(null!));
    }

    /// <summary>
    ///     GetBrookName should return the brook name from the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameReturnsAttributeValue()
    {
        string brookName = BrookNameHelper.GetBrookName<DecoratedType>();
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     GetBrookName should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BrookNameHelper.GetBrookName<UndecoratedType>());
    }

    /// <summary>
    ///     GetBrookName with Type parameter should return the brook name from the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameWithTypeReturnsAttributeValue()
    {
        string brookName = BrookNameHelper.GetBrookName(DecoratedType.AsType);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     GetBrookName with Type parameter should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetBrookNameWithTypeThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BrookNameHelper.GetBrookName(UndecoratedType.AsType));
    }

    /// <summary>
    ///     GetBrookName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void GetBrookNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => BrookNameHelper.GetBrookName(null!));
    }

    /// <summary>
    ///     GetDefinition should return the BrookNameAttribute from the decorated type.
    /// </summary>
    [Fact]
    public void GetDefinitionReturnsAttribute()
    {
        BrookNameAttribute attribute = BrookNameHelper.GetDefinition(DecoratedType.AsType);
        Assert.NotNull(attribute);
        Assert.Equal("APP.MODULE.STREAM", attribute.BrookName);
    }

    /// <summary>
    ///     GetDefinition should throw when attribute is missing.
    /// </summary>
    [Fact]
    public void GetDefinitionThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BrookNameHelper.GetDefinition(UndecoratedType.AsType));
    }

    /// <summary>
    ///     GetDefinition should throw when type is null.
    /// </summary>
    [Fact]
    public void GetDefinitionThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => BrookNameHelper.GetDefinition(null!));
    }

    /// <summary>
    ///     TryGetBrookName should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetBrookNameReturnsFalseWhenAttributeMissing()
    {
        bool result = BrookNameHelper.TryGetBrookName<UndecoratedType>(out string? brookName);
        Assert.False(result);
        Assert.Null(brookName);
    }

    /// <summary>
    ///     TryGetBrookName should return true and the brook name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetBrookNameReturnsTrueWhenAttributeExists()
    {
        bool result = BrookNameHelper.TryGetBrookName<DecoratedType>(out string? brookName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     TryGetBrookName with Type parameter should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetBrookNameWithTypeReturnsFalseWhenAttributeMissing()
    {
        bool result = BrookNameHelper.TryGetBrookName(UndecoratedType.AsType, out string? brookName);
        Assert.False(result);
        Assert.Null(brookName);
    }

    /// <summary>
    ///     TryGetBrookName with Type parameter should return true and the brook name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetBrookNameWithTypeReturnsTrueWhenAttributeExists()
    {
        bool result = BrookNameHelper.TryGetBrookName(DecoratedType.AsType, out string? brookName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.STREAM", brookName);
    }

    /// <summary>
    ///     TryGetBrookName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void TryGetBrookNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => BrookNameHelper.TryGetBrookName(null!, out string? _));
    }
}