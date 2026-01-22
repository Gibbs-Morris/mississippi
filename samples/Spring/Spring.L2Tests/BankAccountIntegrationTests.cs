using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;


namespace Spring.L2Tests;

/// <summary>
///     End-to-end integration tests for the BankAccount aggregate and projection.
///     Tests the full flow from API commands through event sourcing to projection query.
/// </summary>
[Collection(SpringTestCollection.Name)]
public sealed class BankAccountIntegrationTests
{
    /// <summary>
    ///     Maximum time to wait for eventual consistency.
    /// </summary>
    private static readonly TimeSpan EventualConsistencyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Polling interval when waiting for projection updates.
    /// </summary>
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    private readonly SpringFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BankAccountIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public BankAccountIntegrationTests(
        SpringFixture fixture
    ) =>
        this.fixture = fixture;

    /// <summary>
    ///     Polls the projection endpoint until the expected balance is reached or timeout occurs.
    /// </summary>
    private static async Task<BankAccountBalanceResponse?> WaitForProjectionAsync(
        HttpClient client,
        string bankAccountId,
        decimal expectedBalance
    )
    {
        DateTime deadline = DateTime.UtcNow.Add(EventualConsistencyTimeout);
        BankAccountBalanceResponse? lastResult = null;
        Uri projectionUri = new($"api/projections/bank-account-balance/{bankAccountId}", UriKind.Relative);
        while (DateTime.UtcNow < deadline)
        {
            using HttpResponseMessage response = await client.GetAsync(projectionUri);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                lastResult = await response.Content.ReadFromJsonAsync<BankAccountBalanceResponse>();
                if (lastResult is not null && (lastResult.Balance == expectedBalance))
                {
                    return lastResult;
                }
            }

            await Task.Delay(PollingInterval);
        }

        // Return the last result even if it doesn't match, so the test can report what was found
        return lastResult;
    }

    /// <summary>
    ///     Response model for the bank account balance projection.
    /// </summary>
    private sealed class BankAccountBalanceResponse
    {
        /// <summary>
        ///     Gets or sets the current balance.
        /// </summary>
        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        /// <summary>
        ///     Gets or sets the account holder name.
        /// </summary>
        [JsonPropertyName("holderName")]
        public string HolderName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets a value indicating whether the account is open.
        /// </summary>
        [JsonPropertyName("isOpen")]
        public bool IsOpen { get; set; }
    }

    /// <summary>
    ///     Verifies the complete bank account flow: open, deposit, deposit, withdraw,
    ///     then query the projection and verify the balance matches expected calculations.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CompleteBankAccountFlowShouldUpdateProjectionCorrectly()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = new()
        {
            BaseAddress = fixture.ServerBaseUri,
        };
        string bankAccountId = $"test-account-{Guid.NewGuid():N}";
        const string holderName = "John Doe";
        const decimal initialDeposit = 100.00m;
        const decimal firstDeposit = 250.00m;
        const decimal secondDeposit = 150.00m;
        const decimal withdrawal = 75.00m;
        decimal expectedBalance = (initialDeposit + firstDeposit + secondDeposit) - withdrawal;

        // Act - Step 1: Open account with initial deposit
        using (HttpResponseMessage openResponse = await client.PostAsJsonAsync(
                   new Uri($"api/aggregates/bank-account/{bankAccountId}/open", UriKind.Relative),
                   new
                   {
                       HolderName = holderName,
                       InitialDeposit = initialDeposit,
                   }))
        {
            openResponse.StatusCode.Should().Be(HttpStatusCode.OK, "opening account should succeed");
        }

        // Act - Step 2: First deposit
        using (HttpResponseMessage deposit1Response = await client.PostAsJsonAsync(
                   new Uri($"api/aggregates/bank-account/{bankAccountId}/deposit", UriKind.Relative),
                   new
                   {
                       Amount = firstDeposit,
                   }))
        {
            deposit1Response.StatusCode.Should().Be(HttpStatusCode.OK, "first deposit should succeed");
        }

        // Act - Step 3: Second deposit
        using (HttpResponseMessage deposit2Response = await client.PostAsJsonAsync(
                   new Uri($"api/aggregates/bank-account/{bankAccountId}/deposit", UriKind.Relative),
                   new
                   {
                       Amount = secondDeposit,
                   }))
        {
            deposit2Response.StatusCode.Should().Be(HttpStatusCode.OK, "second deposit should succeed");
        }

        // Act - Step 4: Withdraw
        using (HttpResponseMessage withdrawResponse = await client.PostAsJsonAsync(
                   new Uri($"api/aggregates/bank-account/{bankAccountId}/withdraw", UriKind.Relative),
                   new
                   {
                       Amount = withdrawal,
                   }))
        {
            withdrawResponse.StatusCode.Should().Be(HttpStatusCode.OK, "withdrawal should succeed");
        }

        // Act - Step 5: Wait for eventual consistency and poll projection
        BankAccountBalanceResponse? projectionResult =
            await WaitForProjectionAsync(client, bankAccountId, expectedBalance);

        // Assert
        projectionResult.Should().NotBeNull("projection should exist after commands");
        projectionResult!.HolderName.Should().Be(holderName);
        projectionResult.IsOpen.Should().BeTrue();
        projectionResult.Balance.Should().Be(expectedBalance, "balance should reflect all transactions");
    }

    /// <summary>
    ///     Verifies that opening an account creates the projection with correct initial state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task OpenAccountShouldCreateProjection()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = new()
        {
            BaseAddress = fixture.ServerBaseUri,
        };
        string bankAccountId = $"test-account-{Guid.NewGuid():N}";
        const string holderName = "Jane Smith";
        const decimal initialDeposit = 500.00m;

        // Act - Open account
        using (HttpResponseMessage openResponse = await client.PostAsJsonAsync(
                   new Uri($"api/aggregates/bank-account/{bankAccountId}/open", UriKind.Relative),
                   new
                   {
                       HolderName = holderName,
                       InitialDeposit = initialDeposit,
                   }))
        {
            openResponse.StatusCode.Should().Be(HttpStatusCode.OK, "opening account should succeed");
        }

        // Act - Wait for projection
        BankAccountBalanceResponse? projectionResult =
            await WaitForProjectionAsync(client, bankAccountId, initialDeposit);

        // Assert
        projectionResult.Should().NotBeNull("projection should exist after opening account");
        projectionResult!.HolderName.Should().Be(holderName);
        projectionResult.IsOpen.Should().BeTrue();
        projectionResult.Balance.Should().Be(initialDeposit);
    }
}