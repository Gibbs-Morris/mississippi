using System;

using Microsoft.AspNetCore.Components;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.BasePaths;

/// <summary>
///     Supplies a navigation base without browser infrastructure.
/// </summary>
internal sealed class BasePathNavigationManager : NavigationManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BasePathNavigationManager" /> class.
    /// </summary>
    /// <param name="baseUri">The configured application base.</param>
    public BasePathNavigationManager(
        string baseUri
    ) =>
        Initialize(baseUri, baseUri + "accounts/current");

    /// <inheritdoc />
    protected override void NavigateToCore(
        string uri,
        bool forceLoad
    ) =>
        throw new NotSupportedException();
}