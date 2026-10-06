using Microsoft.AspNetCore.Components;

using Mississippi.Inlet.Client.Abstractions.Commands;
using Mississippi.Inlet.Client.Abstractions.State;


namespace MississippiSamples.Spring.Client.Components.Organisms;

/// <summary>
///     Shows generated command responses without claiming that projections have caught up.
/// </summary>
/// <remarks>Public so Spring pages can compose this presentational organism in Razor markup.</remarks>
public sealed partial class CommandActivity
{
    /// <summary>Gets or sets the explanation of what a response proves for this journey.</summary>
    [Parameter]
    public string Explanation { get; set; } = "Check the live projection to verify the resulting outcome.";

    /// <summary>Gets or sets the accessible label for this command feature.</summary>
    [Parameter]
    public string Label { get; set; } = "Command responses";

    /// <summary>Gets or sets the generated command feature state.</summary>
    [Parameter]
    [EditorRequired]
    public AggregateCommandStateBase State { get; set; } = default!;

    private static string ResponseText(
        CommandStatus status
    ) =>
        status switch
        {
            CommandStatus.Succeeded => "Accepted",
            CommandStatus.Failed => "Rejected",
            var _ => "Awaiting response",
        };
}