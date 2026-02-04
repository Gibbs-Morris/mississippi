using Microsoft.AspNetCore.Components;


namespace Spring.Client.Components.Organisms;

/// <summary>
///     Demo accounts control panel.
/// </summary>
public sealed partial class DemoAccountsSection
{
    /// <summary>Gets or sets the demo account A identifier.</summary>
    [Parameter]
    public string? AccountAId { get; set; }

    /// <summary>Gets or sets the demo account A name.</summary>
    [Parameter]
    public string? AccountAName { get; set; }

    /// <summary>Gets or sets the demo account B identifier.</summary>
    [Parameter]
    public string? AccountBId { get; set; }

    /// <summary>Gets or sets the demo account B name.</summary>
    [Parameter]
    public string? AccountBName { get; set; }

    /// <summary>Gets or sets a value indicating whether initialization is in progress.</summary>
    [Parameter]
    public bool IsExecutingOrLoading { get; set; }

    /// <summary>Gets or sets a value indicating whether demo accounts are initialized.</summary>
    [Parameter]
    public bool IsInitialized { get; set; }

    /// <summary>Gets or sets the initialize callback.</summary>
    [Parameter]
    public EventCallback OnInitialize { get; set; }
}