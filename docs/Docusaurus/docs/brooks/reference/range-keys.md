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

## Construction And Parsing

The constructor accepts the brook name, entity ID, start, and count. `FromBrookCompositeKey()` supplies the name and ID from a `BrookKey` and takes start and count separately. `ToBrookCompositeKey()` returns the name and ID without the range fields.

`FromString()` and the implicit conversion from a string parse the four components, then call the constructor. The constructor applies the same validation to directly constructed and parsed keys:

- Brook name and entity ID must be non-null and must not contain `|`. This key type does not reject empty strings.
- Start and count must be nonnegative.
- The combined length of the names, numeric components, and three separators must not exceed `4192` characters.

The separator is a delimiter; the key type does not provide an escaping scheme for names containing it.

## Failure Behavior

- A null name, entity ID, or input string throws `ArgumentNullException`.
- A separator in a name or a combined key length above the limit throws `ArgumentException`.
- A negative start or count throws `ArgumentOutOfRangeException`, including after successful numeric parsing.
- Missing separators or a numeric component that cannot be parsed as `long` throws `FormatException`.

Parsing uses the default `long.TryParse` overload. These rules describe the key representation; the reader's slice-size validation is documented separately in [Brooks Reader Options](./reader-options.md).

## Summary

A range key encodes `brookName|entityId|start|count`. For a nonempty range, the inclusive end is one less than start plus count. Construction and parsing share component, numeric, and length validation.

## Next Steps

- Read [Brooks Reader Options](./reader-options.md) for how readers partition ranges.
- Use [Brooks Reference](./reference.md) for the surrounding event-stream contracts.
