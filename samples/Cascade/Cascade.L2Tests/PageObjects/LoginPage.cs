// <copyright file="LoginPage.cs" company="Corsair Software Ltd">
// Copyright (c) Corsair Software Ltd. All rights reserved.
// Licensed under the Apache-2.0 License. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.Playwright;


namespace Cascade.L2Tests.PageObjects;

/// <summary>
///     Page object for the login page.
/// </summary>
#pragma warning disable CA1515 // Types can be made internal - used by public test classes
internal sealed class LoginPage
#pragma warning restore CA1515
{
    private readonly IPage page;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoginPage" /> class.
    /// </summary>
    /// <param name="page">The Playwright page.</param>
    public LoginPage(
        IPage page
    ) =>
        this.page = page;

    /// <summary>
    ///     Checks if the login page is visible.
    /// </summary>
    /// <returns>True if the login input is visible.</returns>
    public async Task<bool> IsVisibleAsync() => await page.IsVisibleAsync("[id='displayName']");

    /// <summary>
    ///     Logs in with the specified display name.
    /// </summary>
    /// <param name="displayName">The display name to use.</param>
    /// <returns>The channel list page after successful login.</returns>
    public async Task<ChannelListPage> LoginAsync(
        string displayName
    )
    {
        await page.FillAsync("[id='displayName']", displayName);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync("**/channels");
        return new(page);
    }
}