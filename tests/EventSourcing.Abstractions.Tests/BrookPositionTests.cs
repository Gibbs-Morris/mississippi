using System;
using Mississippi.EventSourcing.Abstractions;
using Xunit;

namespace Mississippi.EventSourcing.Abstractions.Tests
{
    /// <summary>
    /// Tests for <see cref="BrookPosition"/> conversion helpers.
    /// </summary>
    public sealed class BrookPositionTests
    {
        /// <summary>
        /// Default constructed position should be NotSet with value -1 and comparisons should respect ordering.
        /// </summary>
        [Fact]
        public void DefaultAndComparisonSemantics()
        {
            var defaultPos = new BrookPosition();
            Assert.True(defaultPos.NotSet);
            Assert.Equal(-1, defaultPos.Value);

            var newer = new BrookPosition(10);
            var older = new BrookPosition(5);
            Assert.True(newer.IsNewerThan(older));
            Assert.False(older.IsNewerThan(newer));
        }

        /// <summary>
        /// Construction with a value less than -1 should throw.
        /// </summary>
        [Fact]
        public void ConstructorLessThanMinusOneThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BrookPosition(-2));
        }

        /// <summary>
        /// Implicit conversions to and from long should preserve value.
        /// </summary>
        [Fact]
        public void ImplicitConversionsPreserveValue()
        {
            BrookPosition implicitFrom = 7;
            Assert.Equal(7, implicitFrom.Value);

            long asLong = implicitFrom;
            Assert.Equal(7L, asLong);
        }

        /// <summary>
        /// FromLong/FromInt64 and conversion helpers should preserve value.
        /// </summary>
        [Fact]
        public void FromAndToConversionsPreserveValue()
        {
            var pFromLong = BrookPosition.FromLong(15);
            Assert.Equal(15, pFromLong.Value);

            var pFromInt64 = BrookPosition.FromInt64(20);
            Assert.Equal(20, pFromInt64.Value);

            long asLong = pFromLong;
            Assert.Equal(15L, asLong);

            Assert.Equal(20L, pFromInt64.ToLong());
            Assert.Equal(20L, pFromInt64.ToInt64());
        }
    }
}
