using System.Collections.Generic;


namespace Mississippi.Architecture.L0Tests.Fixtures.Dependencies;

/// <summary>Negative fixture proving collection mutation retains constructor parameter provenance.</summary>
internal sealed class CollectionAddAssignmentFixture
{
    private readonly List<IClockFixture> clocks = new();

    /// <summary>Initializes a new instance of the <see cref="CollectionAddAssignmentFixture" /> class.</summary>
    /// <param name="clock">The dependency added to the collection.</param>
    public CollectionAddAssignmentFixture(
        IClockFixture clock
    ) =>
        clocks.Add(clock);

    /// <summary>Gets the captured dependency count.</summary>
    public int Count => clocks.Count;
}