using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;


namespace MississippiSamples.Spring.Client.Components.Organisms;

/// <summary>
///     Page header and navigation for the bank account demo.
/// </summary>
public sealed partial class BankAccountPageHeader
{
    private ElementReference connectionButton;

    private bool wasConnectionDetailsOpen;

    /// <summary>Gets or sets the API docs URL.</summary>
    [Parameter]
    public Uri ApiDocsUrl { get; set; } = new("/scalar/v1", UriKind.Relative);

    /// <summary>Gets or sets the connection status text.</summary>
    [Parameter]
    public string ConnectionStatusText { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether connection details are expanded.</summary>
    [Parameter]
    public bool IsConnectionDetailsOpen { get; set; }

    /// <summary>Gets or sets the callback to navigate to investigations.</summary>
    [Parameter]
    public EventCallback OnNavigateInvestigations { get; set; }

    /// <summary>Gets or sets the callback to toggle the connection details.</summary>
    [Parameter]
    public EventCallback OnToggleConnectionDetails { get; set; }

    /// <summary>Gets or sets the subtitle text.</summary>
    [Parameter]
    public string Subtitle { get; set; } = "Mississippi Event Sourcing Demo";

    /// <summary>Gets or sets the tip text.</summary>
    [Parameter]
    public string TipText { get; set; } =
        "Transactions over £10,000 are automatically flagged for investigation. Try it out!";

    /// <summary>Gets or sets the title text.</summary>
    [Parameter]
    public string Title { get; set; } = "Move money";

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(
        bool firstRender
    )
    {
        if (wasConnectionDetailsOpen && !IsConnectionDetailsOpen)
        {
            await connectionButton.FocusAsync();
        }

        wasConnectionDetailsOpen = IsConnectionDetailsOpen;
    }
}