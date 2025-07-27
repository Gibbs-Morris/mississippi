﻿namespace Mississippi.Core.Abstractions.Providers.Serialization;

/// <summary>
/// Provides synchronous serialization functionality for writing objects to byte arrays.
/// </summary>
public interface ISerializationWriter
{
    /// <summary>
    /// Serializes an object to a byte array. This is a pure, in-memory operation that never blocks on I/O.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A byte array containing the serialized data.</returns>
    ReadOnlyMemory<byte> Write<T>(T value);
}