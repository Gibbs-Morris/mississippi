namespace Cascade.Client.Chat;

/// <summary>
///     Contains reducer functions for the chat feature state.
/// </summary>
/// <remarks>
///     Note: Channel and message data now comes from InletStore projections, not reducers.
///     These reducers handle authentication and UI state only.
/// </remarks>
internal static class ChatReducers
{
    /// <summary>
    ///     Reducer for hiding the create channel modal.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The hide modal action.</param>
    /// <returns>The new chat state with the modal hidden.</returns>
    public static ChatState HideCreateChannelModal(
        ChatState state,
        HideCreateChannelModalAction action
    ) =>
        state with
        {
            ShowCreateChannelModal = false,
        };

    /// <summary>
    ///     Reducer for handling login failures.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The login failed action.</param>
    /// <returns>The new chat state with error information.</returns>
    public static ChatState LoginFailed(
        ChatState state,
        LoginFailedAction action
    ) =>
        state with
        {
            IsLoggingIn = false,
            Error = action.Error,
        };

    /// <summary>
    ///     Reducer for setting login in progress state.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The login in progress action.</param>
    /// <returns>The new chat state with loading indicator.</returns>
    public static ChatState LoginInProgress(
        ChatState state,
        LoginInProgressAction action
    ) =>
        state with
        {
            IsLoggingIn = true,
            Error = null,
        };

    /// <summary>
    ///     Reducer for successful login.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The login success action.</param>
    /// <returns>The new chat state with authenticated user.</returns>
    public static ChatState LoginSuccess(
        ChatState state,
        LoginSuccessAction action
    ) =>
        state with
        {
            IsLoggingIn = false,
            IsAuthenticated = true,
            UserId = action.UserId,
            UserDisplayName = action.DisplayName,
            Error = null,
        };

    /// <summary>
    ///     Reducer for logging out.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The logout action.</param>
    /// <returns>A fresh chat state.</returns>
    public static ChatState Logout(
        ChatState state,
        LogoutAction action
    ) =>
        new();

    /// <summary>
    ///     Reducer for selecting a channel.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The select channel action.</param>
    /// <returns>The new chat state with the selected channel.</returns>
    public static ChatState SelectChannel(
        ChatState state,
        SelectChannelAction action
    ) =>
        state with
        {
            SelectedChannelId = action.ChannelId,
        };

    /// <summary>
    ///     Reducer for showing the create channel modal.
    /// </summary>
    /// <param name="state">The current chat state.</param>
    /// <param name="action">The show modal action.</param>
    /// <returns>The new chat state with the modal shown.</returns>
    public static ChatState ShowCreateChannelModal(
        ChatState state,
        ShowCreateChannelModalAction action
    ) =>
        state with
        {
            ShowCreateChannelModal = true,
        };
}