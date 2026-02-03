using Microsoft.AspNetCore.Components;


namespace Spring.Client.Components.Organisms;

/// <summary>
///     Account selection section for entering an account identifier.
/// </summary>
public sealed partial class AccountSelectionSection
{
    /// <summary>Gets or sets the account identifier input.</summary>
    [Parameter]
    public string AccountIdInput { get; set; } = string.Empty;

    /// <summary>Gets or sets the callback when account identifier changes.</summary>
    [Parameter]
    public EventCallback<string> AccountIdInputChanged { get; set; }

    /// <summary>Gets or sets the continue callback.</summary>
    [Parameter]
    public EventCallback OnContinue { get; set; }
}