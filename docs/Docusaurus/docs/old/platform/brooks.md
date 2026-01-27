---
sidebar_position: 3
title: Brooks
description: High-level overview of Mississippi event streams for append and read access.
---

# Brooks

Brooks are Mississippi’s event streams. They provide append-only write operations and read access through Orleans grains.

## Purpose

- Append events with optimistic concurrency.
- Read events in batch or via specialized reader grains.
- Provide a consistent stream abstraction for aggregates and projections.

## Where it fits

Brooks sit between aggregates and projections. Aggregates write events to brooks, and projection readers consume those events.

## Source code reference

- [IBrookWriterGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Writer/IBrookWriterGrain.cs)
- [IBrookReaderGrain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Brooks.Abstractions/Reader/IBrookReaderGrain.cs)
