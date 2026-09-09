using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Xunit;
using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness;

/// <summary>
///     Compares expected public data members with exact collection cardinality.
/// </summary>
internal static class StructuralAssertions
{
    /// <summary>
    ///     Asserts structural equivalence, optionally preserving nested sequence order.
    /// </summary>
    /// <param name="expected">The expected data and members.</param>
    /// <param name="actual">The data to inspect.</param>
    /// <param name="ordered">Whether sequence elements must occur in the expected order.</param>
    internal static void Equivalent(
        object? expected,
        object? actual,
        bool ordered = false
    ) =>
        Equivalent(expected, actual, ordered, new(ReferenceEqualityComparer.Instance));

    private static void Equivalent(
        object? expected,
        object? actual,
        bool ordered,
        HashSet<object> ancestors
    )
    {
        if (ReferenceEquals(expected, actual))
        {
            return;
        }

        if (expected is null ||
            (Type.GetTypeCode(expected.GetType()) != TypeCode.Object) ||
            expected is Guid or DateTimeOffset or TimeSpan or DateOnly or TimeOnly or Uri or Version or Type)
        {
            Assert.Equivalent(expected, actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.True(ancestors.Add(expected), "Cyclic data cannot be compared structurally.");
        try
        {
            if (expected is IDictionary expectedDictionary)
            {
                IDictionary actualDictionary = Assert.IsAssignableFrom<IDictionary>(actual);
                Assert.Equal(expectedDictionary.Count, actualDictionary.Count);
                foreach (DictionaryEntry entry in expectedDictionary)
                {
                    Assert.True(actualDictionary.Contains(entry.Key), $"Expected dictionary key: {entry.Key}");
                    Equivalent(entry.Value, actualDictionary[entry.Key], ordered, ancestors);
                }

                return;
            }

            if (expected is IEnumerable expectedSequence)
            {
                IEnumerable actualSequence = Assert.IsAssignableFrom<IEnumerable>(actual);
                object?[] expectedItems = expectedSequence.Cast<object?>().ToArray();
                List<object?> actualItems = actualSequence.Cast<object?>().ToList();
                Assert.Equal(expectedItems.Length, actualItems.Count);
                if (ordered || expected is byte[])
                {
                    for (int index = 0; index < expectedItems.Length; index++)
                    {
                        Equivalent(expectedItems[index], actualItems[index], ordered, ancestors);
                    }

                    return;
                }

                int[] matches = Enumerable.Repeat(-1, actualItems.Count).ToArray();
                for (int index = 0; index < expectedItems.Length; index++)
                {
                    Assert.True(
                        MatchElement(index, new bool[actualItems.Count]),
                        $"No equivalent collection element was found at expected index {index}.");
                }

                return;

                // Reassign earlier matches when expected member subsets overlap.
                bool MatchElement(
                    int expectedIndex,
                    bool[] visited
                )
                {
                    for (int actualIndex = 0; actualIndex < actualItems.Count; actualIndex++)
                    {
                        if (visited[actualIndex] ||
                            !Matches(expectedItems[expectedIndex], actualItems[actualIndex], ancestors))
                        {
                            continue;
                        }

                        visited[actualIndex] = true;
                        if ((matches[actualIndex] < 0) || MatchElement(matches[actualIndex], visited))
                        {
                            matches[actualIndex] = expectedIndex;
                            return true;
                        }
                    }

                    return false;
                }
            }

            MemberInfo[] expectedMembers = GetDataMembers(expected.GetType());
            if (expectedMembers.Length == 0)
            {
                Assert.Equivalent(expected, actual);
                return;
            }

            MemberInfo[] actualMembers = GetDataMembers(actual.GetType());
            foreach (MemberInfo expectedMember in expectedMembers)
            {
                MemberInfo? actualMember = actualMembers.FirstOrDefault(member => member.Name == expectedMember.Name);
                Assert.True(actualMember is not null, $"Expected public member {expectedMember.Name} was not found.");
                Equivalent(GetValue(expectedMember, expected), GetValue(actualMember, actual), ordered, ancestors);
            }
        }
        finally
        {
            ancestors.Remove(expected);
        }
    }

    private static MemberInfo[] GetDataMembers(
        Type type
    ) =>
        type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Where(member => member is FieldInfo ||
                             (member is PropertyInfo { GetMethod.IsPublic: true } property &&
                              (property.GetIndexParameters().Length == 0)))
            .ToArray();

    private static object? GetValue(
        MemberInfo member,
        object instance
    ) =>
        member is PropertyInfo property ? property.GetValue(instance) : ((FieldInfo)member).GetValue(instance);

    private static bool Matches(
        object? expected,
        object? actual,
        HashSet<object> ancestors
    )
    {
        try
        {
            Equivalent(expected, actual, false, ancestors);
            return true;
        }
        catch (XunitException)
        {
            return false;
        }
    }
}