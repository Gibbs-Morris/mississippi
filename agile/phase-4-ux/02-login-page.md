# Task 4.2: Login Page

**Status**: ⬜ Not Started  
**Depends On**: [3.3 Blazor Server](../phase-3-infrastructure/03-blazor-server.md)

## Goal

Create a simple login page where users enter a display name (no password) to identify themselves for the chat demo.

## Acceptance Criteria

- [ ] Login page with display name input
- [ ] Submit registers user (or finds existing) via `RegisterUser` command
- [ ] UserId stored in session state (cookie or session)
- [ ] Redirect to Channels page on success
- [ ] Error display for failures
- [ ] Cannot access other pages without login
- [ ] L0 tests for session service logic

## Design

### Login Flow

1. User navigates to `/login` (or is redirected there)
2. User enters display name
3. Click "Join Chat" button
4. Server calls `RegisterUser` command (idempotent - same name reuses existing user)
5. On success, store UserId in session
6. Redirect to `/channels`

### User Session Service

```csharp
public interface IUserSession
{
    string? UserId { get; }
    string? DisplayName { get; }
    bool IsLoggedIn { get; }
    
    Task LoginAsync(string displayName, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}

public class UserSession : IUserSession
{
    private IClusterClient ClusterClient { get; }
    private ProtectedSessionStorage SessionStorage { get; }
    
    public string? UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsLoggedIn => UserId is not null;
    
    public async Task LoginAsync(string displayName, CancellationToken ct = default)
    {
        // Generate or lookup UserId
        string userId = $"user-{displayName.ToLowerInvariant().Replace(" ", "-")}";
        
        // Register user via aggregate
        BrookKey brookKey = BrookKey.For<UserBrook>(userId);
        IUserAggregateGrain userGrain = ClusterClient.GetGrain<IUserAggregateGrain>(brookKey);
        
        OperationResult result = await userGrain.ExecuteAsync(
            new RegisterUser { UserId = userId, DisplayName = displayName });
        
        // Ignore "already registered" error - that's fine
        if (result.IsFailure && result.Error.Code != UserErrorCodes.AlreadyRegistered)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
        
        // Store in session
        UserId = userId;
        DisplayName = displayName;
        await SessionStorage.SetAsync("userId", userId);
        await SessionStorage.SetAsync("displayName", displayName);
    }
}
```

### Login.razor Component

```razor
@page "/login"
@inject IUserSession UserSession
@inject NavigationManager Navigation

<div class="login-container">
    <h1>Welcome to Cascade Chat</h1>
    
    <EditForm Model="loginModel" OnValidSubmit="HandleLogin">
        <DataAnnotationsValidator />
        
        <div class="form-group">
            <label for="displayName">Display Name</label>
            <InputText id="displayName" @bind-Value="loginModel.DisplayName" 
                       class="form-control" placeholder="Enter your name" />
            <ValidationMessage For="() => loginModel.DisplayName" />
        </div>
        
        @if (errorMessage is not null)
        {
            <div class="alert alert-danger">@errorMessage</div>
        }
        
        <button type="submit" class="btn btn-primary" disabled="@isLoading">
            @if (isLoading)
            {
                <span class="spinner-border spinner-border-sm"></span>
            }
            else
            {
                <span>Join Chat</span>
            }
        </button>
    </EditForm>
</div>

@code {
    private LoginModel loginModel = new();
    private bool isLoading;
    private string? errorMessage;
    
    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;
        
        try
        {
            await UserSession.LoginAsync(loginModel.DisplayName);
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
    
    public class LoginModel
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string DisplayName { get; set; } = "";
    }
}
```

### Auth Check Component

```razor
<!-- In MainLayout.razor or Routes.razor -->
@inject IUserSession UserSession
@inject NavigationManager Navigation

@if (!UserSession.IsLoggedIn)
{
    Navigation.NavigateTo("/login");
}
else
{
    @Body
}
```

## TDD Steps

1. **Red**: Create `UserSessionTests`
   - Test: `LoginAsync_RegistersNewUser`
   - Test: `LoginAsync_ExistingUser_Succeeds`
   - Test: `LoginAsync_StoresInSessionStorage`
   - Test: `IsLoggedIn_WhenUserIdSet_ReturnsTrue`

2. **Green**: Implement `UserSession` service

3. **Red**: Create `LoginPageTests` (bUnit)
   - Test: `Submit_WithValidName_RedirectsToChannels`
   - Test: `Submit_WithEmptyName_ShowsValidationError`

4. **Green**: Implement Login.razor

## Files to Create

- `samples/Cascade/Cascade.Server/Components/Services/IUserSession.cs`
- `samples/Cascade/Cascade.Server/Components/Services/UserSession.cs`
- `samples/Cascade/Cascade.Server/Components/Pages/Login.razor`
- `samples/Cascade/Cascade.Domain.L0Tests/Services/UserSessionTests.cs`

## Notes

- No real authentication; this is demo-only
- UserId derived from display name for simplicity
- Consider adding logout button in layout
- `ProtectedSessionStorage` encrypts values in browser session
