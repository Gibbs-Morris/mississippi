---
title: Brooks Range Keys
description: Reference the Brooks range-key fields, inclusive endpoints, string format, and validation rules.
sidebar_position: 3
sidebar_label: Range Keys
---

# Brooks Range Keys

`BrookRangeKey` identifies a range within one brook using a starting position and an event count. Its `End` property is inclusive.

## Applies To

- `Mississippi.Brooks.Abstractions.BrookRangeKey`
- Conversion to and from `Mississippi.Brooks.Abstractions.BrookKey`

## Fields And String Format

The [range-key contract](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookRangeKey.cs) exposes these read-only properties:

| Property | Meaning |
|----------|---------|
| `BrookName` | Name of the brook |
| `EntityId` | Entity identifier within that brook |
| `Start` | First position, represented by `BrookPosition` |
| `Count` | Number of positions in the range, represented by `long` |
| `End` | Computed inclusive end: `Start + Count - 1` |

`ToString()`, the implicit string conversion, and `FromBrookRangeKey()` use the format `brookName|entityId|start|count`.

For example, `orders|order-42|5|10` has a start of `5` and a count of `10`, so its positions run from `5` through `14`. The last component is a count, not the ending position.

## Zero Count

The constructor allows a count of `0`. In that case, `End` equals `Start - 1`: a start of `10` and count of `0` computes an end of `9`.

With start `0` and count `0`, the end is `-1`, the unset value supported by [BrookPosition](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/BrookPosition.cs). Do not interpret the computed end of a zero-count range as an included event position.

The [existing range-key tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Abstractions.L0Tests/BrookRangeKeyTests.cs) verify both the inclusive-end calculation and the zero-count case.

## Endpoint Arithmetic

For a nonempty range, choose start and count so the inclusive end is representable as a nonnegative `long`. The constructor validates start and count separately; it does not validate this arithmetic constraint.

`End` calculates `(Start + Count) - 1` using unchecked `long` arithmetic, then converts the result to `BrookPosition`. A start of `long.MaxValue` and count of `2` passes construction, but the calculation wraps to `long.MinValue`. Accessing `End` then throws `ArgumentOutOfRangeException` because that position is below `-1`.

## Construction And Parsing

The constructor accepts the brook name, entity ID, start, and count. `FromBrookCompositeKey()` supplies the name and ID from a `BrookKey` and takes start and count separately. `ToBrookCompositeKey()` constructs a `BrookKey` from the name and ID without the range fields. Both helpers run the destination key's constructor validation.

A valid `BrookKey` can still exceed the range-key limit after the numeric fields and extra separators are added. For example, a name of `4191` UTF-16 code units with an empty entity ID uses the stream key's full `4192`-unit allowance. Converting it with start `0` and count `0` requires `4196` units and throws `ArgumentException`.

`FromString()` and the implicit conversion from a string parse the four components, then call the constructor. The constructor applies the same validation to directly constructed and parsed keys:

- Brook name and entity ID must be non-null and must not contain `|`. This key type does not reject empty strings.
- Start and count must be nonnegative.
- The length check counts the names, invariant-culture decimal representations of the parsed start and count, and three separators. That normalized length must not exceed `4192` UTF-16 code units, the unit counted by [`string.Length`](https://learn.microsoft.com/en-us/dotnet/api/system.string.length).

Numeric fields are parsed before the length check, so their surrounding whitespace and leading zeroes do not count toward the limit. The original input string can exceed `4192` UTF-16 code units when its normalized components pass this check. A supplementary Unicode character occupies two code units and therefore uses two units of the limit.

The separator is a delimiter; the key type does not provide an escaping scheme for names containing it.

`default(BrookRangeKey)` and parameterless `new BrookRangeKey()` bypass the validating four-argument constructor. Their name and entity ID are null, and start and count are zero. Use the constructor or parsing methods when you need the validated representation described above.

## Failure Behavior

- Passing a null name or entity ID to the four-argument constructor, or a null input string to `FromString()`, throws `ArgumentNullException`.
- Passing a name containing `|` directly to the constructor throws `ArgumentException`. The constructor also throws `ArgumentException` when the normalized key length exceeds the limit.
- A negative start or count throws `ArgumentOutOfRangeException`, including after successful numeric parsing.
- Accessing `End` throws `ArgumentOutOfRangeException` if arithmetic overflow produces a position below `-1`.
- Parsing a string with missing or extra separators, or a numeric component that cannot be parsed as `long`, throws `FormatException`. For example, `a|b|c|1|2` cannot be parsed as a range key; embedded separators are not treated as escaped name characters.

Parsing uses the default `long.TryParse` overload. These rules describe the key representation; the reader's slice-size validation is documented separately in [Brooks Reader Options](./reader-options.md).

## Summary

A range key encodes `brookName|entityId|start|count`. For a nonempty range, the inclusive end is one less than start plus count. Construction and parsing share component, numeric, and length validation.

## Next Steps

- Read [Brooks Reader Options](./reader-options.md) for how readers partition ranges.
- Use [Brooks Reference](./reference.md) for the surrounding event-stream contracts.
