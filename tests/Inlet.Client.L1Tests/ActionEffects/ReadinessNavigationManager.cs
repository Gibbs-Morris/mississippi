using System;

using Microsoft.AspNetCore.Components;


namespace MississippiTests.Inlet.Client.L1Tests.ActionEffects;

/// <summary>
///     Resolves the production provider's hub URL against the owned test server.
/// </summary>
internal sealed class ReadinessNavigationManager : NavigationManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReadinessNavigationManager" /> class.
    /// </summary>
    /// <param name="address">The test server's base address.</param>
    internal ReadinessNavigationManager(
        string address
    ) =>
        Initialize(address + "/", address + "/");

    /// <inheritdoc />
    protected override void NavigateToCore(
        string uri,
        bool forceLoad
    ) =>
        throw new NotSupportedException();
}