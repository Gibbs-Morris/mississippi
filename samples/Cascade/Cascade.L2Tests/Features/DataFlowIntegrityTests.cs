using Allure.Xunit.Attributes;

using Cascade.L2Tests.PageObjects;

using Microsoft.Playwright;


namespace Cascade.L2Tests.Features;

/// <summary>
///     Tests that verify the complete data flow through the system:
///     UX → Aggregate Grain → Brook (Cosmos storage) → Projection → UX.
/// </summary>
/// <remarks>
///     <para>
///         These tests ensure data integrity across the event-sourcing pipeline:
///         1. User input from the UI triggers a command.
///         2. Command is processed by the aggregate grain.
///         3. Event is stored in a brook (Cosmos DB).
///         4. Projection reads and processes the event.
///         5. UI receives and displays the updated projection.
///     </para>
/// </remarks>
[AllureParentSuite("Cascade Chat App")]
[AllureSuite("Data Flow Integrity")]
[AllureSubSuite("End-to-End Data Flow")]
public class DataFlowIntegrityTests : TestBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DataFlowIntegrityTests" /> class.
    /// </summary>
    /// <param name="fixture">The Playwright fixture.</param>
    public DataFlowIntegrityTests(
        PlaywrightFixture fixture
    )
        : base(fixture)
    {
    }

    /// <summary>
    ///     Verifies that channel creation flows through the aggregate and appears in the channel list.
    ///     This tests the Channel aggregate → UserChannelList projection pipeline.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Channel Data Flow")]
    public async Task ChannelCreationFlowsThroughAggregateToProjection()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("ChannelCreator");
        ChannelListPage channelList = new(page);
        string uniqueId = Guid.NewGuid().ToString("N")[..8];
        string channelName = $"flow-test-{uniqueId}";

        // Act - Create channel (triggers: UX → CreateChannel command → ChannelAggregate → ChannelCreated event)
        await channelList.CreateChannelAsync(channelName);

        // Assert - Channel appears in list (verifies: Brook → UserChannelListProjection → UX)
        IReadOnlyList<string> channels = await channelList.GetChannelNamesAsync();
        Assert.Contains(channels, c => c.Contains(uniqueId, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that data persists in the brook by refreshing the page
    ///     and confirming the data is still available from the projection.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Data Persistence")]
    public async Task DataPersistsInBrookAndSurvivesPageRefresh()
    {
        // Arrange - Create channel and send message
        IPage page = await CreatePageAndLoginAsync("PersistenceUser");
        ChannelListPage channelList = new(page);
        string persistenceId = Guid.NewGuid().ToString("N")[..8];
        string channelName = $"persist-{persistenceId}";
        ChannelViewPage channelView = await channelList.CreateChannelAsync(channelName);
        string messageContent = $"Persistence test [{persistenceId}]";
        await channelView.SendMessageAsync(messageContent);
        await channelView.WaitForMessageAsync(messageContent, 15000);

        // Act - Refresh the page (forces re-reading from projection/brook)
        await page.ReloadAsync();

        // Wait for page to load and navigate back to channel
        await page.WaitForURLAsync(
            "**/channels/**",
            new()
            {
                Timeout = 10000,
            });
        channelList = new(page);
        channelView = await channelList.SelectChannelAsync(channelName);

        // Assert - Message should still be visible (confirms brook persistence)
        await channelView.WaitForMessageAsync(messageContent, 15000);
        IReadOnlyList<string> messages = await channelView.GetMessagesAsync();
        Assert.Contains(messages, m => m.Contains(persistenceId, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies message content integrity through the entire pipeline.
    ///     Checks that special characters, unicode, and whitespace are preserved.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Content Integrity")]
    public async Task MessageContentIntegrityThroughPipeline()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("ContentUser");
        ChannelListPage channelList = new(page);
        string channelName = $"content-{Guid.NewGuid():N}"[..24];
        ChannelViewPage channelView = await channelList.CreateChannelAsync(channelName);

        // Test messages with various content that could be corrupted
        string[] testMessages =
        [
            "Simple message",
            "Message with numbers 12345",
            "Message with punctuation! @#$%^&*()",
            "Message with unicode: café, naïve, résumé",
            "Message with emoji: Hello 👋 World 🌍",
        ];

        // Act & Assert - Each message should round-trip correctly
        foreach (string originalMessage in testMessages)
        {
            await channelView.SendMessageAsync(originalMessage);
            await channelView.WaitForMessageAsync(originalMessage, 15000);
        }

        // Verify all messages are present
        IReadOnlyList<string> displayedMessages = await channelView.GetMessagesAsync();
        foreach (string expected in testMessages)
        {
            Assert.Contains(displayedMessages, m => m.Contains(expected, StringComparison.Ordinal));
        }
    }

    /// <summary>
    ///     Verifies that a message sent from the UI flows through the aggregate,
    ///     is persisted in the brook, and appears in the UI via the projection.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Message Data Flow")]
    public async Task MessageSentFromUiAppearsInProjectionAndUi()
    {
        // Arrange - Create a unique message that we can track through the system
        IPage page = await CreatePageAndLoginAsync("DataFlowUser1");
        ChannelListPage channelList = new(page);
        string channelName = $"dataflow-{Guid.NewGuid():N}"[..24];
        ChannelViewPage channelView = await channelList.CreateChannelAsync(channelName);
        string uniqueMessageId = Guid.NewGuid().ToString("N");
        string messageContent = $"DataFlow test message [{uniqueMessageId}]";

        // Act - Send message through the UI (triggers: UX → Command → Aggregate)
        await channelView.SendMessageAsync(messageContent);

        // Assert - Wait for message to appear (verifies: Brook → Projection → UX)
        // The message appearing confirms the entire pipeline worked:
        // 1. UX captured the input
        // 2. Command was dispatched to ConversationAggregateGrain
        // 3. MessageSent event was persisted to Brook (Cosmos)
        // 4. ChannelMessagesProjection processed the event
        // 5. SignalR pushed the update back to the UI
        await channelView.WaitForMessageAsync(messageContent, 15000);
        IReadOnlyList<string> messages = await channelView.GetMessagesAsync();
        Assert.Contains(messages, m => m.Contains(uniqueMessageId, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that multiple messages maintain their order through the event-sourcing pipeline.
    ///     This confirms that brook append-only ordering is preserved in projections.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Message Ordering")]
    public async Task MessagesPreserveOrderThroughEventSourcingPipeline()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("OrderTestUser");
        ChannelListPage channelList = new(page);
        string channelName = $"order-test-{Guid.NewGuid():N}"[..24];
        ChannelViewPage channelView = await channelList.CreateChannelAsync(channelName);
        string batchId = Guid.NewGuid().ToString("N")[..8];
        string[] expectedMessages =
        [
            $"[{batchId}] Message 1 - First in sequence",
            $"[{batchId}] Message 2 - Second in sequence",
            $"[{batchId}] Message 3 - Third in sequence",
        ];

        // Act - Send messages sequentially
        foreach (string message in expectedMessages)
        {
            await channelView.SendMessageAsync(message);
            await channelView.WaitForMessageAsync(message, 15000);
        }

        // Assert - Verify all messages appear and in correct order
        IReadOnlyList<string> displayedMessages = await channelView.GetMessagesAsync();

        // Find our test messages and check their relative order
        int[] indices = expectedMessages.Select(expected => displayedMessages.Select((
                    msg,
                    idx
                ) => (msg, idx))
                .Where(tuple => tuple.msg.Contains(batchId, StringComparison.Ordinal) &&
                                tuple.msg.Contains(expected, StringComparison.Ordinal))
                .Select(tuple => tuple.idx)
                .FirstOrDefault(-1))
            .ToArray();

        // All messages should be found
        Assert.All(indices, idx => Assert.True(idx >= 0, "Message should be found in UI"));

        // Messages should be in ascending order (first message has lowest index)
        Assert.True(indices[0] < indices[1], "First message should appear before second");
        Assert.True(indices[1] < indices[2], "Second message should appear before third");
    }

    /// <summary>
    ///     Verifies that the message count in the projection matches the number
    ///     of messages sent, confirming no events are lost or duplicated.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Event Counting")]
    public async Task ProjectionMessageCountMatchesSentMessages()
    {
        // Arrange
        IPage page = await CreatePageAndLoginAsync("CountUser");
        ChannelListPage channelList = new(page);
        string channelName = $"count-{Guid.NewGuid():N}"[..24];
        ChannelViewPage channelView = await channelList.CreateChannelAsync(channelName);
        const int expectedCount = 5;
        string countId = Guid.NewGuid().ToString("N")[..8];

        // Act - Send exactly N messages
        for (int i = 1; i <= expectedCount; i++)
        {
            string message = $"[{countId}] Count test message {i}";
            await channelView.SendMessageAsync(message);
            await channelView.WaitForMessageAsync(message, 15000);
        }

        // Assert - Exactly N messages with our ID should be in the projection
        IReadOnlyList<string> messages = await channelView.GetMessagesAsync();
        int actualCount = messages.Count(m => m.Contains(countId, StringComparison.Ordinal));
        Assert.Equal(expectedCount, actualCount);
    }

    /// <summary>
    ///     Verifies real-time updates: when User1 sends a message,
    ///     User2 sees it without refreshing (via SignalR projection push).
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Real-Time Data Flow")]
    public async Task RealTimeProjectionUpdatesReachSecondUser()
    {
        // Arrange - Both users in the same channel
        IPage page1 = await CreatePageAndLoginAsync("RealTimeSender");
        ChannelListPage channelList1 = new(page1);
        string realtimeId = Guid.NewGuid().ToString("N")[..8];
        string channelName = $"realtime-{realtimeId}";
        ChannelViewPage channelView1 = await channelList1.CreateChannelAsync(channelName);

        // Second user joins
        IPage page2 = await CreatePageAndLoginAsync("RealTimeReceiver");
        ChannelListPage channelList2 = new(page2);
        await page2.WaitForSelectorAsync(
            $".channel-item:has-text('{channelName}')",
            new()
            {
                Timeout = 15000,
            });
        ChannelViewPage channelView2 = await channelList2.SelectChannelAsync(channelName);

        // Act - First user sends a NEW message while second user is watching
        string liveMessage = $"Live update [{realtimeId}]";
        await channelView1.SendMessageAsync(liveMessage);

        // Assert - Second user should receive the message in real-time via SignalR
        // (No page refresh - this verifies the projection push pipeline)
        await channelView2.WaitForMessageAsync(liveMessage, 15000);
        IReadOnlyList<string> page2Messages = await channelView2.GetMessagesAsync();
        Assert.Contains(page2Messages, m => m.Contains(liveMessage, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Verifies that a second user can see messages sent by the first user,
    ///     confirming the projection is shared across clients.
    /// </summary>
    /// <returns>A task representing the async test.</returns>
    [Fact]
    [AllureFeature("Multi-User Data Flow")]
    public async Task SecondUserSeesMessagesFromFirstUser()
    {
        // Arrange - First user creates channel and sends message
        IPage page1 = await CreatePageAndLoginAsync("Sender");
        ChannelListPage channelList1 = new(page1);
        string sharedId = Guid.NewGuid().ToString("N")[..8];
        string channelName = $"shared-{sharedId}";
        ChannelViewPage channelView1 = await channelList1.CreateChannelAsync(channelName);
        string messageFromUser1 = $"Hello from User1 [{sharedId}]";
        await channelView1.SendMessageAsync(messageFromUser1);
        await channelView1.WaitForMessageAsync(messageFromUser1, 15000);

        // Act - Second user joins the same channel
        IPage page2 = await CreatePageAndLoginAsync("Receiver");
        ChannelListPage channelList2 = new(page2);

        // Second user needs to join/select the same channel
        // Wait for channel to appear in list (via projection)
        await page2.WaitForSelectorAsync(
            $".channel-item:has-text('{channelName}')",
            new()
            {
                Timeout = 15000,
            });
        ChannelViewPage channelView2 = await channelList2.SelectChannelAsync(channelName);

        // Assert - Second user should see the message from first user
        await channelView2.WaitForMessageAsync(messageFromUser1, 15000);
        IReadOnlyList<string> messages = await channelView2.GetMessagesAsync();
        Assert.Contains(messages, m => m.Contains(sharedId, StringComparison.Ordinal));
    }
}