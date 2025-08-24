using System;
using System.Collections.Immutable;
using Mississippi.EventSourcing.Abstractions;
using Xunit;

namespace Mississippi.EventSourcing.Abstractions.Tests
{
    /// <summary>
    /// Tests for the <see cref="BrookEvent"/> record.
    /// </summary>
    public class BrookEventTests
    {
        /// <summary>
        /// Default-constructed values are empty or null as defined.
        /// </summary>
        [Fact]
        public void DefaultValuesAreEmptyOrNull()
        {
            var e = new BrookEvent();

            Assert.Equal(string.Empty, e.Type);
            Assert.Equal(string.Empty, e.Source);
            Assert.Equal(string.Empty, e.Id);
            Assert.Equal(string.Empty, e.DataContentType);
            Assert.True(e.Data.IsDefaultOrEmpty);
            Assert.Null(e.Time);
        }

        /// <summary>
        /// Record initializer sets provided values.
        /// </summary>
        [Fact]
        public void InitializeWithValues()
        {
            var data = ImmutableArray.Create((byte)1, (byte)2);
            var now = DateTimeOffset.UtcNow;
            var e = new BrookEvent
            {
                Type = "T",
                Source = "S",
                Id = "I",
                DataContentType = "application/json",
                Data = data,
                Time = now,
            };

            Assert.Equal("T", e.Type);
            Assert.Equal("S", e.Source);
            Assert.Equal("I", e.Id);
            Assert.Equal("application/json", e.DataContentType);
            Assert.Equal(data, e.Data);
            Assert.Equal(now, e.Time);
        }
    }
}
