using System;

using Microsoft.AspNetCore.Components;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects;

/// <summary>
///     Supplies the provider URL for tests canceled before any network request.
/// </summary>
internal sealed class FailedStartupNavigationManager : NavigationManager
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FailedStartupNavigationManager" /> class.
    /// </summary>
    public FailedStartupNavigationManager() => Initialize("http://localhost/", "http://localhost/");

    /// <inheritdoc />
    protected override void NavigateToCore(
        string uri,
        bool forceLoad
    ) =>
        throw new NotSupportedException();
}