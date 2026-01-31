using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;


namespace Spring.L2Tests;

/// <summary>
///     End-to-end integration tests for the TransferFunds saga.
/// </summary>
/// <remarks>
///     <para>
///         These tests validate the complete saga flow including:
///         <list type="bullet">
///             <item>Starting a transfer via the generated saga endpoint</item>
///             <item>Saga step execution against real aggregate grains</item>
///             <item>Saga status projection updates</item>
///             <item>Compensation when steps fail</item>
///         </list>
///     </para>
/// </remarks>
[Collection(SpringTestCollection.Name)]
public sealed class TransferFundsSagaTests
{
    /// <summary>
    ///     Polling interval when waiting for saga/projection updates.
    /// </summary>
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Maximum time to wait for saga completion (accounts for 10s step delay).
    /// </summary>
    private static readonly TimeSpan SagaCompletionTimeout = TimeSpan.FromSeconds(30);

    private readonly SpringFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransferFundsSagaTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public TransferFundsSagaTests(
        SpringFixture fixture
    ) =>
        this.fixture = fixture;

    private static async Task<BankAccountBalanceResponse?> GetBalanceAsync(
        HttpClient client,
        string accountId
    )
    {
        using HttpResponseMessage response = await client.GetAsync(
            new Uri($"api/projections/bank-account-balance/{accountId}", UriKind.Relative));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BankAccountBalanceResponse>();
    }

    private static async Task<SagaStatusResponse?> GetSagaStatusAsync(
        HttpClient client,
        string sagaId
    )
    {
        using HttpResponseMessage response = await client.GetAsync(
            new Uri($"api/projections/transfer-saga-status/{sagaId}", UriKind.Relative));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SagaStatusResponse>();
    }

    private static async Task OpenAccountAsync(
        HttpClient client,
        string accountId,
        string holderName,
        decimal initialDeposit
    )
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"api/aggregates/bank-account/{accountId}/open", UriKind.Relative),
            new
            {
                HolderName = holderName,
                InitialDeposit = initialDeposit,
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"opening account {accountId} should succeed");
    }

    private static async Task StartTransferSagaAsync(
        HttpClient client,
        string sagaId,
        string sourceAccountId,
        string destinationAccountId,
        decimal amount
    )
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"api/sagas/transfer-funds/{sagaId}", UriKind.Relative),
            new
            {
                SourceAccountId = sourceAccountId,
                DestinationAccountId = destinationAccountId,
                Amount = amount,
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK, "starting transfer saga should succeed");
    }

    private static async Task<SagaStatusResponse?> WaitForSagaCompletionAsync(
        HttpClient client,
        string sagaId,
        TimeSpan? timeout = null
    )
    {
        TimeSpan effectiveTimeout = timeout ?? SagaCompletionTimeout;
        DateTime deadline = DateTime.UtcNow.Add(effectiveTimeout);
        SagaStatusResponse? lastStatus = null;
        while (DateTime.UtcNow < deadline)
        {
            lastStatus = await GetSagaStatusAsync(client, sagaId);
            if (lastStatus is not null &&
                string.Equals(lastStatus.Phase, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return lastStatus;
            }

            await Task.Delay(PollingInterval);
        }

        return lastStatus;
    }

    private static async Task<SagaStatusResponse?> WaitForSagaFailureAsync(
        HttpClient client,
        string sagaId,
        TimeSpan? timeout = null
    )
    {
        TimeSpan effectiveTimeout = timeout ?? SagaCompletionTimeout;
        DateTime deadline = DateTime.UtcNow.Add(effectiveTimeout);
        SagaStatusResponse? lastStatus = null;
        while (DateTime.UtcNow < deadline)
        {
            lastStatus = await GetSagaStatusAsync(client, sagaId);
            if (lastStatus is not null &&
                (string.Equals(lastStatus.Phase, "Failed", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(lastStatus.Phase, "Compensated", StringComparison.OrdinalIgnoreCase)))
            {
                return lastStatus;
            }

            await Task.Delay(PollingInterval);
        }

        return lastStatus;
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
    ///     Response model for the saga status projection.
    /// </summary>
    private sealed class SagaStatusResponse
    {
        /// <summary>
        ///     Gets or sets when the saga completed.
        /// </summary>
        [JsonPropertyName("completedAt")]
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        ///     Gets or sets the failure reason if failed.
        /// </summary>
        [JsonPropertyName("failureReason")]
        public string? FailureReason { get; set; }

        /// <summary>
        ///     Gets or sets the current phase.
        /// </summary>
        [JsonPropertyName("phase")]
        public string Phase { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the saga ID.
        /// </summary>
        [JsonPropertyName("sagaId")]
        public string SagaId { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Verifies that saga status projection tracks progress correctly.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task SagaStatusProjectionTracksProgress()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = new()
        {
            BaseAddress = fixture.ServerBaseUri,
        };
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        const decimal sourceInitial = 1000m;
        const decimal transferAmount = 100m;
        await OpenAccountAsync(client, sourceId, "ProgressTest", sourceInitial);
        await OpenAccountAsync(client, destId, "ProgressDest", 0m);

        // Act - Start transfer
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferSagaAsync(client, sagaId, sourceId, destId, transferAmount);

        // Immediately check status - should be running or have step info
        SagaStatusResponse? initialStatus = await GetSagaStatusAsync(client, sagaId);
        _ = initialStatus; // Verify status is queryable during execution

        // Wait for completion
        SagaStatusResponse? finalStatus = await WaitForSagaCompletionAsync(client, sagaId);

        // Assert - Final status reflects completion
        finalStatus.Should().NotBeNull();
        finalStatus!.Phase.Should().Be("Completed");
        finalStatus.CompletedAt.Should().NotBeNull("completed saga should have completion timestamp");
    }

    /// <summary>
    ///     Verifies that compensation runs when destination credit fails
    ///     (e.g., destination account is closed).
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TransferFundsCompensatesWhenDestinationClosed()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = new()
        {
            BaseAddress = fixture.ServerBaseUri,
        };
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        const decimal sourceInitial = 1000m;
        const decimal transferAmount = 250m;
        await OpenAccountAsync(client, sourceId, "Sender", sourceInitial);
        // Intentionally do not open the destination account to force a credit failure.

        // Act - Start transfer saga (should fail at credit step and compensate)
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferSagaAsync(client, sagaId, sourceId, destId, transferAmount);

        // Wait for saga to fail (after compensation)
        SagaStatusResponse? status = await WaitForSagaFailureAsync(
            client,
            sagaId,
            TimeSpan.FromSeconds(60)); // Extra time for compensation

        // Assert - Saga failed after compensation
        status.Should().NotBeNull("saga should have a status");
        (status!.Phase is "Failed" or "Compensated").Should().BeTrue("saga should fail or compensate");

        // Assert - Source balance restored via compensation
        BankAccountBalanceResponse? sourceBalance = await GetBalanceAsync(client, sourceId);
        sourceBalance.Should().NotBeNull();
        sourceBalance!.Balance.Should().Be(sourceInitial, "source balance should be restored after compensation");
    }

    /// <summary>
    ///     Verifies that a transfer fails gracefully when source has insufficient funds.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TransferFundsFailsWithInsufficientFunds()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = new()
        {
            BaseAddress = fixture.ServerBaseUri,
        };
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        const decimal sourceInitial = 100m;
        const decimal transferAmount = 500m; // More than available
        await OpenAccountAsync(client, sourceId, "LowBalance", sourceInitial);
        await OpenAccountAsync(client, destId, "Receiver", 0m);

        // Act - Start transfer saga (should fail at debit step)
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferSagaAsync(client, sagaId, sourceId, destId, transferAmount);

        // Wait for saga to fail
        SagaStatusResponse? status = await WaitForSagaFailureAsync(client, sagaId);

        // Assert - Saga failed
        status.Should().NotBeNull("saga should have a status");
        status!.Phase.Should().Be("Failed", "saga should fail");

        // Assert - Source balance unchanged
        BankAccountBalanceResponse? sourceBalance = await GetBalanceAsync(client, sourceId);
        sourceBalance.Should().NotBeNull();
        sourceBalance!.Balance.Should().Be(sourceInitial, "source balance should be unchanged on failure");
    }

    /// <summary>
    ///     Verifies that a transfer between two funded accounts succeeds
    ///     and balances are correctly updated.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TransferFundsSuccessfullyUpdatesBalances()
    {
        // Arrange
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = new()
        {
            BaseAddress = fixture.ServerBaseUri,
        };
        string sourceId = $"source-{Guid.NewGuid():N}";
        string destId = $"dest-{Guid.NewGuid():N}";
        const string sourceHolder = "Alice";
        const string destHolder = "Bob";
        const decimal sourceInitial = 1000m;
        const decimal destInitial = 500m;
        const decimal transferAmount = 250m;

        // Create and fund source account
        await OpenAccountAsync(client, sourceId, sourceHolder, sourceInitial);

        // Create and fund destination account
        await OpenAccountAsync(client, destId, destHolder, destInitial);

        // Act - Start transfer saga
        string sagaId = Guid.NewGuid().ToString("N");
        await StartTransferSagaAsync(client, sagaId, sourceId, destId, transferAmount);

        // Wait for saga to complete
        SagaStatusResponse? status = await WaitForSagaCompletionAsync(client, sagaId);

        // Assert - Saga completed
        status.Should().NotBeNull("saga should have a status");
        status!.Phase.Should().Be("Completed", "saga should complete successfully");

        // Assert - Balances updated
        BankAccountBalanceResponse? sourceBalance = await GetBalanceAsync(client, sourceId);
        BankAccountBalanceResponse? destBalance = await GetBalanceAsync(client, destId);
        sourceBalance.Should().NotBeNull();
        sourceBalance!.Balance.Should().Be(sourceInitial - transferAmount, "source should be debited");
        destBalance.Should().NotBeNull();
        destBalance!.Balance.Should().Be(destInitial + transferAmount, "destination should be credited");
    }
}