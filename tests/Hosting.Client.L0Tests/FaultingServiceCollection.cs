using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Hosting.Client.L0Tests;

/// <summary>
///     Injects deterministic failures after host service publication begins.
/// </summary>
internal sealed class FaultingServiceCollection
    : Collection<ServiceDescriptor>,
      IServiceCollection
{
    /// <summary>
    ///     Gets the failures to throw on matching publication operations.
    /// </summary>
    public Queue<Exception> Failures { get; } = new();

    /// <summary>
    ///     Gets or sets a value indicating whether clearing, rather than inserting, should fail.
    /// </summary>
    public bool ShouldFailOnClear { get; set; }

    private bool HasPublicationStarted { get; set; }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        HasPublicationStarted = true;
        base.ClearItems();
        ThrowIfRequested(true);
    }

    /// <inheritdoc />
    protected override void InsertItem(
        int index,
        ServiceDescriptor item
    )
    {
        ThrowIfRequested(false);
        base.InsertItem(index, item);
    }

    private void ThrowIfRequested(
        bool isClear
    )
    {
        if (HasPublicationStarted && (ShouldFailOnClear == isClear) && Failures.TryDequeue(out Exception? failure))
        {
            throw failure;
        }
    }
}