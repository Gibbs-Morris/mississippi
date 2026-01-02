# Task 5.1: Playwright L2 Tests via Aspire

**Status**: ✅ Complete  
**Depends On**: All previous phases

## Goal

Create a Playwright-based E2E test project that validates the complete Cascade chat application with multi-user real-time scenarios. **Tests run via Aspire orchestration** using `Aspire.Hosting.Testing`.

## Test Execution Architecture

```
┌───────────────────────────────────────────────────────────────────────────────┐
│                    TEST EXECUTION (via Aspire)                                │
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │ Cascade.L2Tests (xUnit + Playwright)                                    │ │
│  │                                                                         │ │
│  │  ┌─────────────────┐                                                    │ │
│  │  │ PlaywrightFixture│◀──────────────────────────────────────────────┐   │ │
│  │  │ (IAsyncLifetime) │                                               │   │ │
│  │  └─────────────────┘                                                │   │ │
│  │          │                                                          │   │ │
│  │          │ 1. Start Aspire AppHost                                  │   │ │
│  │          ▼                                                          │   │ │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │ │
│  │  │ DistributedApplicationTestingBuilder                        │    │   │ │
│  │  │   .CreateAsync<Projects.Cascade_AppHost>()                  │    │   │ │
│  │  │   .BuildAsync() → .StartAsync()                             │    │   │ │
│  │  └─────────────────────────────────────────────────────────────┘    │   │ │
│  │          │                                                          │   │ │
│  │          │ 2. Aspire starts all resources                          │   │ │
│  │          ▼                                                          │   │ │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │ │
│  │  │                   ASPIRE ORCHESTRATES                          │ │   │ │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌────────────────────────┐ │ │   │ │
│  │  │  │  Azurite    │  │ Cosmos DB   │  │ Cascade.Server         │ │ │   │ │
│  │  │  │ (container) │  │ Emulator    │  │ (Blazor + Orleans)     │ │ │   │ │
│  │  │  └─────────────┘  └─────────────┘  └────────────────────────┘ │ │   │ │
│  │  └────────────────────────────────────────────────────────────────┘ │   │ │
│  │          │                                                          │   │ │
│  │          │ 3. Get BaseUrl from resource                            │   │ │
│  │          ▼                                                          │   │ │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │ │
│  │  │ app.GetResourceBuilder("cascade-server")                    │    │   │ │
│  │  │   .GetEndpoint("https").GetUriAsync() → BaseUrl             │    │   │ │
│  │  └─────────────────────────────────────────────────────────────┘    │   │ │
│  │          │                                                          │   │ │
│  │          │ 4. Playwright tests use BaseUrl                          │   │ │
│  │          ▼                                                          │   │ │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │ │
│  │  │ Test Methods (RealTimeTests, LoginTests, etc.)              │    │   │ │
│  │  │   page.GotoAsync(Fixture.BaseUrl)                           │    │   │ │
│  │  └─────────────────────────────────────────────────────────────┘    │   │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
│  5. Fixture.DisposeAsync() stops Aspire app + containers                     │
└───────────────────────────────────────────────────────────────────────────────┘
```

## Acceptance Criteria

- [x] Test project `Cascade.L2Tests` created with Playwright + Aspire.Hosting.Testing packages
- [x] **Test fixture starts full Aspire AppHost** (Cosmos Emulator, Azurite, Cascade.Server)
- [x] Tests wait for all resources to be healthy before executing
- [x] Page objects abstract Blazor component interactions
- [x] Multi-browser test for real-time message delivery
- [x] Tests are deterministic and isolated
- [x] Proper cleanup via Aspire `StopAsync`/`DisposeAsync`
- [x] Added to `samples.sln` and `samples.slnx`

## Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Aspire.Hosting.Testing" />
    <PackageReference Include="Allure.Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Cascade.AppHost\Cascade.AppHost.csproj" />
  </ItemGroup>
</Project>
```

## Test Infrastructure

### PlaywrightFixture.cs

```csharp
using Aspire.Hosting.Testing;
using Microsoft.Playwright;

public class PlaywrightFixture : IAsyncLifetime
{
    private DistributedApplication? app;
    private IPlaywright? playwright;
    
    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; private set; } = "";
    
    public async Task InitializeAsync()
    {
        // Start Aspire AppHost
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Cascade_AppHost>();
        
        app = await appHost.BuildAsync();
        await app.StartAsync();
        
        // Get the Cascade.Server URL
        var serverResource = app.GetResourceBuilder("cascade-server");
        BaseUrl = await serverResource.GetEndpoint("https").GetUriAsync();
        
        // Initialize Playwright
        playwright = await Playwright.CreateAsync();
        Browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }
    
    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        playwright?.Dispose();
        
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
    
    public async Task<IPage> CreatePageAsync()
    {
        var context = await Browser.NewContextAsync();
        return await context.NewPageAsync();
    }
}
```

### TestBase.cs

```csharp
[AllureParentSuite("Cascade")]
[AllureSuite("E2E Tests")]
public abstract class TestBase : IClassFixture<PlaywrightFixture>
{
    protected PlaywrightFixture Fixture { get; }
    
    protected TestBase(PlaywrightFixture fixture)
    {
        Fixture = fixture;
    }
    
    protected async Task<IPage> CreatePageAndLoginAsync(string displayName)
    {
        var page = await Fixture.CreatePageAsync();
        await page.GotoAsync(Fixture.BaseUrl);
        
        // Should redirect to login
        await page.WaitForURLAsync("**/login");
        
        // Enter display name
        await page.FillAsync("[id='displayName']", displayName);
        await page.ClickAsync("button[type='submit']");
        
        // Wait for redirect to channels
        await page.WaitForURLAsync("**/channels");
        
        return page;
    }
}
```

## Page Objects

### LoginPage.cs

```csharp
public class LoginPage
{
    private readonly IPage page;
    
    public LoginPage(IPage page) => this.page = page;
    
    public async Task<ChannelListPage> LoginAsync(string displayName)
    {
        await page.FillAsync("[id='displayName']", displayName);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync("**/channels");
        return new ChannelListPage(page);
    }
    
    public async Task<bool> IsVisibleAsync()
    {
        return await page.IsVisibleAsync("[id='displayName']");
    }
}
```

### ChannelListPage.cs

```csharp
public class ChannelListPage
{
    private readonly IPage page;
    
    public ChannelListPage(IPage page) => this.page = page;
    
    public async Task<ChannelViewPage> CreateChannelAsync(string name)
    {
        await page.ClickAsync(".channel-list-header button");
        await page.FillAsync("input[name='Name']", name);
        await page.ClickAsync("button:has-text('Create')");
        await page.WaitForSelectorAsync($".channel-item:has-text('{name}')");
        return new ChannelViewPage(page);
    }
    
    public async Task<ChannelViewPage> SelectChannelAsync(string name)
    {
        await page.ClickAsync($".channel-item:has-text('{name}')");
        return new ChannelViewPage(page);
    }
    
    public async Task<IReadOnlyList<string>> GetChannelNamesAsync()
    {
        var items = await page.QuerySelectorAllAsync(".channel-item .channel-name");
        var names = new List<string>();
        foreach (var item in items)
        {
            names.Add(await item.TextContentAsync() ?? "");
        }
        return names;
    }
}
```

### ChannelViewPage.cs

```csharp
public class ChannelViewPage
{
    private readonly IPage page;
    
    public ChannelViewPage(IPage page) => this.page = page;
    
    public async Task SendMessageAsync(string content)
    {
        await page.FillAsync(".message-input input", content);
        await page.ClickAsync(".message-input button");
    }
    
    public async Task<IReadOnlyList<string>> GetMessagesAsync()
    {
        var items = await page.QuerySelectorAllAsync(".message-item .message-content");
        var messages = new List<string>();
        foreach (var item in items)
        {
            messages.Add(await item.TextContentAsync() ?? "");
        }
        return messages;
    }
    
    public async Task WaitForMessageAsync(string content, int timeoutMs = 5000)
    {
        await page.WaitForSelectorAsync(
            $".message-item:has-text('{content}')", 
            new() { Timeout = timeoutMs });
    }
}
```

## Test Cases

### LoginTests.cs

```csharp
[AllureSubSuite("Login")]
public class LoginTests : TestBase
{
    public LoginTests(PlaywrightFixture fixture) : base(fixture) { }
    
    [Fact]
    public async Task Navigate_WithoutLogin_RedirectsToLogin()
    {
        var page = await Fixture.CreatePageAsync();
        await page.GotoAsync(Fixture.BaseUrl + "/channels");
        await page.WaitForURLAsync("**/login");
        
        Assert.Contains("/login", page.Url);
    }
    
    [Fact]
    public async Task Login_WithValidName_RedirectsToChannels()
    {
        var page = await Fixture.CreatePageAsync();
        await page.GotoAsync(Fixture.BaseUrl);
        
        var loginPage = new LoginPage(page);
        await loginPage.LoginAsync("TestUser");
        
        Assert.Contains("/channels", page.Url);
    }
}
```

### RealTimeTests.cs

```csharp
[AllureSubSuite("Real-Time")]
public class RealTimeTests : TestBase
{
    public RealTimeTests(PlaywrightFixture fixture) : base(fixture) { }
    
    [Fact]
    public async Task SendMessage_AppearsInOtherUserView_InRealTime()
    {
        // Setup: Two users in same channel
        var pageAlice = await CreatePageAndLoginAsync("Alice");
        var pageBob = await CreatePageAndLoginAsync("Bob");
        
        // Alice creates channel
        var aliceChannelList = new ChannelListPage(pageAlice);
        var aliceChannelView = await aliceChannelList.CreateChannelAsync("test-channel");
        
        // Bob joins channel (via known ID or search - simplified for test)
        var bobChannelList = new ChannelListPage(pageBob);
        var bobChannelView = await bobChannelList.SelectChannelAsync("test-channel");
        
        // Alice sends message
        await aliceChannelView.SendMessageAsync("Hello from Alice!");
        
        // Bob should see it appear in real-time
        await bobChannelView.WaitForMessageAsync("Hello from Alice!", timeoutMs: 5000);
        
        var bobMessages = await bobChannelView.GetMessagesAsync();
        Assert.Contains("Hello from Alice!", bobMessages);
    }
}
```

## TDD Steps

1. **Red**: Create empty test project, write first test that fails (app doesn't exist yet)

2. **Green**: Once all previous phases complete, tests should pass

3. **Refactor**: Extract common patterns into page objects and base class

## Files to Create

- `samples/Cascade/Cascade.L2Tests/Cascade.L2Tests.csproj`
- `samples/Cascade/Cascade.L2Tests/GlobalUsings.cs`
- `samples/Cascade/Cascade.L2Tests/PlaywrightFixture.cs`
- `samples/Cascade/Cascade.L2Tests/TestBase.cs`
- `samples/Cascade/Cascade.L2Tests/PageObjects/LoginPage.cs`
- `samples/Cascade/Cascade.L2Tests/PageObjects/ChannelListPage.cs`
- `samples/Cascade/Cascade.L2Tests/PageObjects/ChannelViewPage.cs`
- `samples/Cascade/Cascade.L2Tests/Features/LoginTests.cs`
- `samples/Cascade/Cascade.L2Tests/Features/ChannelCreationTests.cs`
- `samples/Cascade/Cascade.L2Tests/Features/MessagingTests.cs`
- `samples/Cascade/Cascade.L2Tests/Features/RealTimeTests.cs`

## Setup Commands

```powershell
# Install Playwright browsers (run once)
pwsh samples/Cascade/Cascade.L2Tests/bin/Debug/net9.0/playwright.ps1 install
```

## CI Considerations

- Playwright runs headless by default
- Aspire containers need Docker/Podman available
- First run may be slow (container image pulls)
- Consider test parallelization limits for resource usage

## Notes

- Tests are intentionally comprehensive to validate the full stack
- Real-time test uses two browser contexts simultaneously
- Timeout of 5 seconds for real-time message delivery is generous but accounts for CI variability
- Clean test data by using unique channel names per test
