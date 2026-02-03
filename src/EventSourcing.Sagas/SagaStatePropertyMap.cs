using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Caches saga state property metadata for mutation.
/// </summary>
internal sealed class SagaStatePropertyMap
{
    private readonly PropertyInfo[] settableProperties;

    private readonly Dictionary<string, PropertyInfo> propertyLookup;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaStatePropertyMap" /> class.
    /// </summary>
    /// <param name="stateType">The saga state type.</param>
    public SagaStatePropertyMap(
        Type stateType
    )
    {
        ArgumentNullException.ThrowIfNull(stateType);
        settableProperties = stateType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite)
            .ToArray();
        propertyLookup = settableProperties.ToDictionary(property => property.Name, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Copies writable property values from the source instance to the target instance.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target object.</param>
    public void CopyValues(
        object source,
        object target
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        foreach (PropertyInfo property in settableProperties)
        {
            object? value = property.GetValue(source);
            property.SetValue(target, value);
        }
    }

    /// <summary>
    ///     Sets a property value on the target saga instance.
    /// </summary>
    /// <param name="target">The target saga state instance.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    public void SetProperty(
        object target,
        string propertyName,
        object? value
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        if (!propertyLookup.TryGetValue(propertyName, out PropertyInfo? property))
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on saga state type.");
        }

        property.SetValue(target, value);
    }
}
