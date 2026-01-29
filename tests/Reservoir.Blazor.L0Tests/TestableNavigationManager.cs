using System.Collections.Generic;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     A testable implementation of <see cref="NavigationManager" /> for unit testing.
/// </summary>
/// <remarks>
///     This class provides a controllable navigation manager that records all navigation
///     calls for verification in tests without requiring browser or Blazor runtime.
/// </remarks>
internal sealed class TestableNavigationManager : NavigationManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestableNavigationManager" /> class.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="uri">The initial current URI.</param>
    public TestableNavigationManager(
        string baseUri = "https://example.com/",
        string uri = "https://example.com/"
    )
    {
        Initialize(baseUri, uri);
    }

    /// <summary>
    ///     Gets the recorded navigation calls.
    /// </summary>
    public IReadOnlyList<NavigationRecord> Navigations => NavigationsInternal;

    private List<NavigationRecord> NavigationsInternal { get; } = [];

    /// <inheritdoc />
    protected override void NavigateToCore(
        string uri,
        NavigationOptions options
    )
    {
        NavigationsInternal.Add(new(uri, options.ForceLoad, options.ReplaceHistoryEntry));

        // Do not call Initialize again - NavigationManager only allows one initialization
        // The navigation record is sufficient for test verification
    }

    /// <summary>
    ///     Record of a navigation call.
    /// </summary>
    /// <param name="Uri">The URI navigated to.</param>
    /// <param name="ForceLoad">Whether force load was requested.</param>
    /// <param name="ReplaceHistoryEntry">Whether history replacement was requested.</param>
    internal sealed record NavigationRecord(string Uri, bool ForceLoad, bool ReplaceHistoryEntry);
}