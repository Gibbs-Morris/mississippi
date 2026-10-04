using System;

using Microsoft.AspNetCore.Components;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Resolves the real provider's hub URL against the test server.
/// </summary>
internal sealed class StartupStatusNavigationManager : NavigationManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StartupStatusNavigationManager" /> class.
    /// </summary>
    /// <param name="baseUri">The test server address.</param>
    public StartupStatusNavigationManager(
        string baseUri
    ) =>
        Initialize(baseUri, baseUri);

    /// <inheritdoc />
    protected override void NavigateToCore(
        string uri,
        bool forceLoad
    ) =>
        throw new NotSupportedException();
}