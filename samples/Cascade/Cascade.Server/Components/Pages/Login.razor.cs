// <copyright file="Login.razor.cs" company="Gibbs-Morris">
//   Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Cascade.Server.Components.Services;

using Microsoft.AspNetCore.Components;


namespace Cascade.Server.Components.Pages;

/// <summary>
///     Page component for user login.
/// </summary>
public sealed partial class Login : ComponentBase
{
    private string? errorMessage;

    private bool isLoading;

    [SupplyParameterFromForm]
    private LoginModel Model { get; set; } = new();

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private UserSession UserSession { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        // If already logged in, redirect to channels
        if (UserSession.IsAuthenticated)
        {
            Navigation.NavigateTo("/channels");
        }
    }

#pragma warning disable CA1031 // Catch general exception for UI error display
    private async Task HandleLoginAsync()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            await UserSession.LoginAsync(Model.DisplayName);
            Navigation.NavigateTo("/channels");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
#pragma warning restore CA1031

    private sealed class LoginModel
    {
        [Required(ErrorMessage = "Please enter a display name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        public string DisplayName { get; set; } = string.Empty;
    }
}