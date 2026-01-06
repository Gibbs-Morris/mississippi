using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Cascade.Components.Services;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Shared;

/// <summary>
///     Modal component for creating a new channel.
/// </summary>
public sealed partial class CreateChannelModal : ComponentBase
{
    private readonly CreateChannelModel model = new();

    private string? error;

    private bool isLoading;

    /// <summary>
    ///     Gets or sets the callback when the modal is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    ///     Gets or sets the callback when a channel is created.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnCreated { get; set; }

    [Inject]
    private IChatService ChatService { get; set; } = default!;

    private async Task CloseAsync() => await OnClose.InvokeAsync();

    private async Task HandleSubmitAsync()
    {
        isLoading = true;
        error = null;
        try
        {
            string channelId = await ChatService.CreateChannelAsync(model.Name, model.Description);
            await OnCreated.InvokeAsync(channelId);
        }
        catch (ChatOperationException ex)
        {
            error = ex.Message;
        }
#pragma warning disable CA1031 // Catch general exception for UI error display
        catch (Exception ex)
#pragma warning restore CA1031
        {
            error = $"An unexpected error occurred: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private sealed class CreateChannelModel
    {
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Channel name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Channel name must be between 2 and 50 characters")]
        public string Name { get; set; } = string.Empty;
    }
}