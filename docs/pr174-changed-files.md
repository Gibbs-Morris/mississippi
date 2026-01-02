# PR #174 Changed Files

This document tracks all files changed in PR #174 compared to the `main` branch.

## Review Process

Files will be reviewed for consistency in:

- XML comments
- Function names
- Class names
- Variable names

Files are marked with:

- `[ ]` - Not yet reviewed
- `[x]` - Reviewed and verified

---

## ✅ Review Complete

**Date**: 2025-01-02  
**Status**: All files reviewed, build verified, L0 tests passed

### Review Summary

- **Files Reviewed**: 257 C# files
- **Files Modified**: 36 files
  - 34 copyright header fixes
  - 2 test key format fixes
- **Build Status**: ✅ Both Mississippi and Samples solutions build with 0 warnings, 0 errors
- **Unit Tests**: ✅ All L0 tests passed

### Issues Found and Fixed

1. **Copyright Headers** (34 files):
   - McLaren Applied Ltd. → Gibbs-Morris (2 files)
   - Corsair Software Ltd → Gibbs-Morris (11 files)
   - GMM → Gibbs-Morris (21 files)

2. **Test Key Format Issues** (2 files):
   - `UxProjectionVersionedCacheGrainTests.cs`: Fixed key format from 4-part to 3-part
   - `UxProjectionCursorGrainIntegrationTests.cs`: Changed from `UxProjectionKey` to `UxProjectionCursorKey`

See [worklog.md](./worklog.md) for detailed change documentation.

---

## Framework Source Files (src/)

### EventSourcing.Aggregates.Abstractions

- [ ] [IAggregateGrain.cs](../src/EventSourcing.Aggregates.Abstractions/IAggregateGrain.cs)
- [ ] [IAggregateGrainFactory.cs](../src/EventSourcing.Aggregates.Abstractions/IAggregateGrainFactory.cs)
- [ ] [IEventTypeRegistry.cs](../src/EventSourcing.Aggregates.Abstractions/IEventTypeRegistry.cs)
- [ ] [ISnapshotTypeRegistry.cs](../src/EventSourcing.Aggregates.Abstractions/ISnapshotTypeRegistry.cs)

### EventSourcing.Aggregates

- [ ] [AggregateGrainBase.cs](../src/EventSourcing.Aggregates/AggregateGrainBase.cs)
- [ ] [AggregateGrainFactory.cs](../src/EventSourcing.Aggregates/AggregateGrainFactory.cs)
- [ ] [AggregateRegistrations.cs](../src/EventSourcing.Aggregates/AggregateRegistrations.cs)
- [ ] [EventTypeRegistry.cs](../src/EventSourcing.Aggregates/EventTypeRegistry.cs)
- [ ] [SnapshotTypeRegistry.cs](../src/EventSourcing.Aggregates/SnapshotTypeRegistry.cs)

### EventSourcing.Brooks.Abstractions

- [ ] [Attributes/EventNameHelper.cs](../src/EventSourcing.Brooks.Abstractions/Attributes/EventNameHelper.cs)
- [ ] [Attributes/EventStorageNameAttribute.cs](../src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameAttribute.cs)
- [ ] [Attributes/EventStorageNameHelper.cs](../src/EventSourcing.Brooks.Abstractions/Attributes/EventStorageNameHelper.cs)
- [ ] [Attributes/SnapshotNameHelper.cs](../src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotNameHelper.cs)
- [ ] [Attributes/SnapshotStorageNameAttribute.cs](../src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotStorageNameAttribute.cs)
- [ ] [Attributes/SnapshotStorageNameHelper.cs](../src/EventSourcing.Brooks.Abstractions/Attributes/SnapshotStorageNameHelper.cs)
- [ ] [BrookAsyncReaderKey.cs](../src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs)
- [ ] [BrookKey.cs](../src/EventSourcing.Brooks.Abstractions/BrookKey.cs)
- [ ] [BrookNameHelper.cs](../src/EventSourcing.Brooks.Abstractions/BrookNameHelper.cs)
- [ ] [BrookRangeKey.cs](../src/EventSourcing.Brooks.Abstractions/BrookRangeKey.cs)
- [ ] [IBrookDefinition.cs](../src/EventSourcing.Brooks.Abstractions/IBrookDefinition.cs)
- [ ] [Storage/IBrookStorageProvider.cs](../src/EventSourcing.Brooks.Abstractions/Storage/IBrookStorageProvider.cs)

### EventSourcing.Snapshots.Abstractions

- [ ] [ISnapshotCacheGrain.cs](../src/EventSourcing.Snapshots.Abstractions/ISnapshotCacheGrain.cs)
- [ ] [ISnapshotPersisterGrain.cs](../src/EventSourcing.Snapshots.Abstractions/ISnapshotPersisterGrain.cs)
- [ ] [SnapshotKey.cs](../src/EventSourcing.Snapshots.Abstractions/SnapshotKey.cs)
- [ ] [SnapshotRetentionOptions.cs](../src/EventSourcing.Snapshots.Abstractions/SnapshotRetentionOptions.cs)
- [ ] [SnapshotStreamKey.cs](../src/EventSourcing.Snapshots.Abstractions/SnapshotStreamKey.cs)

### EventSourcing.Snapshots.Cosmos

- [ ] [Mapping/SnapshotDocumentToStorageMapper.cs](../src/EventSourcing.Snapshots.Cosmos/Mapping/SnapshotDocumentToStorageMapper.cs)
- [ ] [Mapping/SnapshotStorageToDocumentMapper.cs](../src/EventSourcing.Snapshots.Cosmos/Mapping/SnapshotStorageToDocumentMapper.cs)
- [ ] [Mapping/SnapshotWriteModelToDocumentMapper.cs](../src/EventSourcing.Snapshots.Cosmos/Mapping/SnapshotWriteModelToDocumentMapper.cs)
- [ ] [Storage/SnapshotDocument.cs](../src/EventSourcing.Snapshots.Cosmos/Storage/SnapshotDocument.cs)

### EventSourcing.Snapshots

- [ ] [SnapshotCacheGrain.cs](../src/EventSourcing.Snapshots/SnapshotCacheGrain.cs)
- [ ] [SnapshotPersisterGrain.cs](../src/EventSourcing.Snapshots/SnapshotPersisterGrain.cs)

### EventSourcing.UxProjections.Abstractions

- [ ] [IUxProjectionCursorGrain.cs](../src/EventSourcing.UxProjections.Abstractions/IUxProjectionCursorGrain.cs)
- [ ] [IUxProjectionGrain.cs](../src/EventSourcing.UxProjections.Abstractions/IUxProjectionGrain.cs)
- [ ] [IUxProjectionGrainFactory.cs](../src/EventSourcing.UxProjections.Abstractions/IUxProjectionGrainFactory.cs)
- [ ] [IUxProjectionVersionedCacheGrain.cs](../src/EventSourcing.UxProjections.Abstractions/IUxProjectionVersionedCacheGrain.cs)
- [ ] [Subscriptions/IUxProjectionSubscriptionGrain.cs](../src/EventSourcing.UxProjections.Abstractions/Subscriptions/IUxProjectionSubscriptionGrain.cs)
- [ ] [Subscriptions/UxProjectionChangedEvent.cs](../src/EventSourcing.UxProjections.Abstractions/Subscriptions/UxProjectionChangedEvent.cs)
- [ ] [Subscriptions/UxProjectionSubscriptionRequest.cs](../src/EventSourcing.UxProjections.Abstractions/Subscriptions/UxProjectionSubscriptionRequest.cs)
- [ ] [UxProjectionCursorKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionCursorKey.cs)
- [ ] [UxProjectionKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionKey.cs)
- [ ] [UxProjectionVersionedCacheKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionVersionedCacheKey.cs)
- [ ] [UxProjectionVersionedKey.cs](../src/EventSourcing.UxProjections.Abstractions/UxProjectionVersionedKey.cs)

### EventSourcing.UxProjections.Api

- [ ] [UxProjectionControllerBase.cs](../src/EventSourcing.UxProjections.Api/UxProjectionControllerBase.cs)
- [ ] [UxProjectionControllerLoggerExtensions.cs](../src/EventSourcing.UxProjections.Api/UxProjectionControllerLoggerExtensions.cs)

### EventSourcing.UxProjections.SignalR

- [ ] [Grains/IUxClientGrain.cs](../src/EventSourcing.UxProjections.SignalR/Grains/IUxClientGrain.cs)
- [ ] [Grains/IUxGroupGrain.cs](../src/EventSourcing.UxProjections.SignalR/Grains/IUxGroupGrain.cs)
- [ ] [Grains/IUxServerDirectoryGrain.cs](../src/EventSourcing.UxProjections.SignalR/Grains/IUxServerDirectoryGrain.cs)
- [ ] [Grains/State/ServerInfo.cs](../src/EventSourcing.UxProjections.SignalR/Grains/State/ServerInfo.cs)
- [ ] [Grains/State/UxClientState.cs](../src/EventSourcing.UxProjections.SignalR/Grains/State/UxClientState.cs)
- [ ] [Grains/State/UxGroupState.cs](../src/EventSourcing.UxProjections.SignalR/Grains/State/UxGroupState.cs)
- [ ] [Grains/State/UxServerDirectoryState.cs](../src/EventSourcing.UxProjections.SignalR/Grains/State/UxServerDirectoryState.cs)
- [ ] [Grains/UxClientGrain.cs](../src/EventSourcing.UxProjections.SignalR/Grains/UxClientGrain.cs)
- [ ] [Grains/UxClientGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections.SignalR/Grains/UxClientGrainLoggerExtensions.cs)
- [ ] [Grains/UxGroupGrain.cs](../src/EventSourcing.UxProjections.SignalR/Grains/UxGroupGrain.cs)
- [ ] [Grains/UxGroupGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections.SignalR/Grains/UxGroupGrainLoggerExtensions.cs)
- [ ] [Grains/UxServerDirectoryGrain.cs](../src/EventSourcing.UxProjections.SignalR/Grains/UxServerDirectoryGrain.cs)
- [ ] [Grains/UxServerDirectoryGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections.SignalR/Grains/UxServerDirectoryGrainLoggerExtensions.cs)
- [ ] [IUxProjectionHubClient.cs](../src/EventSourcing.UxProjections.SignalR/IUxProjectionHubClient.cs)
- [ ] [UxProjectionHub.cs](../src/EventSourcing.UxProjections.SignalR/UxProjectionHub.cs)
- [ ] [UxProjectionHubLoggerExtensions.cs](../src/EventSourcing.UxProjections.SignalR/UxProjectionHubLoggerExtensions.cs)
- [ ] [UxProjectionSignalRServiceCollectionExtensions.cs](../src/EventSourcing.UxProjections.SignalR/UxProjectionSignalRServiceCollectionExtensions.cs)

### EventSourcing.UxProjections

- [ ] [Subscriptions/ActiveSubscription.cs](../src/EventSourcing.UxProjections/Subscriptions/ActiveSubscription.cs)
- [ ] [Subscriptions/UxProjectionSubscriptionGrain.cs](../src/EventSourcing.UxProjections/Subscriptions/UxProjectionSubscriptionGrain.cs)
- [ ] [Subscriptions/UxProjectionSubscriptionGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections/Subscriptions/UxProjectionSubscriptionGrainLoggerExtensions.cs)
- [ ] [UxProjectionCursorGrain.cs](../src/EventSourcing.UxProjections/UxProjectionCursorGrain.cs)
- [ ] [UxProjectionCursorGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections/UxProjectionCursorGrainLoggerExtensions.cs)
- [ ] [UxProjectionGrainBase.cs](../src/EventSourcing.UxProjections/UxProjectionGrainBase.cs)
- [ ] [UxProjectionGrainFactory.cs](../src/EventSourcing.UxProjections/UxProjectionGrainFactory.cs)
- [ ] [UxProjectionGrainFactoryLoggerExtensions.cs](../src/EventSourcing.UxProjections/UxProjectionGrainFactoryLoggerExtensions.cs)
- [ ] [UxProjectionGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections/UxProjectionGrainLoggerExtensions.cs)
- [ ] [UxProjectionVersionedCacheGrain.cs](../src/EventSourcing.UxProjections/UxProjectionVersionedCacheGrain.cs)
- [ ] [UxProjectionVersionedCacheGrainLoggerExtensions.cs](../src/EventSourcing.UxProjections/UxProjectionVersionedCacheGrainLoggerExtensions.cs)

---

## Framework Test Files (tests/)

### EventSourcing.Aggregates.L0Tests

- [ ] [AggregateGrainFactoryTests.cs](../tests/EventSourcing.Aggregates.L0Tests/AggregateGrainFactoryTests.cs)
- [ ] [AggregateGrainTestAggregate.cs](../tests/EventSourcing.Aggregates.L0Tests/AggregateGrainTestAggregate.cs)
- [ ] [AggregateGrainTestBrook.cs](../tests/EventSourcing.Aggregates.L0Tests/AggregateGrainTestBrook.cs)
- [ ] [AggregateGrainTestEvent.cs](../tests/EventSourcing.Aggregates.L0Tests/AggregateGrainTestEvent.cs)
- [ ] [AggregateGrainTests.cs](../tests/EventSourcing.Aggregates.L0Tests/AggregateGrainTests.cs)
- [ ] [AggregateRegistrationsTests.cs](../tests/EventSourcing.Aggregates.L0Tests/AggregateRegistrationsTests.cs)

### EventSourcing.Brooks.Abstractions.L0Tests

- [ ] [Attributes/EventNameHelperTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/Attributes/EventNameHelperTests.cs)
- [ ] [Attributes/EventStorageNameAttributeTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/Attributes/EventStorageNameAttributeTests.cs)
- [ ] [Attributes/EventStorageNameHelperTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/Attributes/EventStorageNameHelperTests.cs)
- [ ] [Attributes/SnapshotNameHelperTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/Attributes/SnapshotNameHelperTests.cs)
- [ ] [Attributes/SnapshotStorageNameAttributeTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/Attributes/SnapshotStorageNameAttributeTests.cs)
- [ ] [Attributes/SnapshotStorageNameHelperTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/Attributes/SnapshotStorageNameHelperTests.cs)
- [ ] [BrookAsyncReaderKeyTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/BrookAsyncReaderKeyTests.cs)
- [ ] [BrookKeyForTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/BrookKeyForTests.cs)
- [ ] [BrookKeyTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/BrookKeyTests.cs)
- [ ] [BrookNameHelperTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/BrookNameHelperTests.cs)
- [ ] [BrookRangeKeyTests.cs](../tests/EventSourcing.Brooks.Abstractions.L0Tests/BrookRangeKeyTests.cs)

### EventSourcing.Snapshots.Abstractions.L0Tests

- [ ] [SnapshotKeyTests.cs](../tests/EventSourcing.Snapshots.Abstractions.L0Tests/SnapshotKeyTests.cs)
- [ ] [SnapshotStreamKeyTests.cs](../tests/EventSourcing.Snapshots.Abstractions.L0Tests/SnapshotStreamKeyTests.cs)

### EventSourcing.Snapshots.Cosmos.L0Tests

- [ ] [SnapshotCosmosRepositoryTests.cs](../tests/EventSourcing.Snapshots.Cosmos.L0Tests/SnapshotCosmosRepositoryTests.cs)
- [ ] [SnapshotMappingTests.cs](../tests/EventSourcing.Snapshots.Cosmos.L0Tests/SnapshotMappingTests.cs)
- [ ] [SnapshotStorageModelTests.cs](../tests/EventSourcing.Snapshots.Cosmos.L0Tests/SnapshotStorageModelTests.cs)
- [ ] [SnapshotStorageProviderTests.cs](../tests/EventSourcing.Snapshots.Cosmos.L0Tests/SnapshotStorageProviderTests.cs)
- [ ] [SnapshotWriteModelTests.cs](../tests/EventSourcing.Snapshots.Cosmos.L0Tests/SnapshotWriteModelTests.cs)

### EventSourcing.Snapshots.L0Tests

- [ ] [SnapshotCacheGrainTestBrook.cs](../tests/EventSourcing.Snapshots.L0Tests/SnapshotCacheGrainTestBrook.cs)
- [ ] [SnapshotCacheGrainTests.cs](../tests/EventSourcing.Snapshots.L0Tests/SnapshotCacheGrainTests.cs)
- [ ] [SnapshotGrainFactoryTests.cs](../tests/EventSourcing.Snapshots.L0Tests/SnapshotGrainFactoryTests.cs)
- [ ] [SnapshotPersisterGrainTests.cs](../tests/EventSourcing.Snapshots.L0Tests/SnapshotPersisterGrainTests.cs)
- [ ] [TestableSnapshotCacheGrain.cs](../tests/EventSourcing.Snapshots.L0Tests/TestableSnapshotCacheGrain.cs)

### EventSourcing.UxProjections.Abstractions.L0Tests

- [ ] [Subscriptions/UxProjectionSubscriptionTypesTests.cs](../tests/EventSourcing.UxProjections.Abstractions.L0Tests/Subscriptions/UxProjectionSubscriptionTypesTests.cs)
- [ ] [UxProjectionKeyForGrainTests.cs](../tests/EventSourcing.UxProjections.Abstractions.L0Tests/UxProjectionKeyForGrainTests.cs)
- [ ] [UxProjectionKeyTests.cs](../tests/EventSourcing.UxProjections.Abstractions.L0Tests/UxProjectionKeyTests.cs)
- [ ] [UxProjectionVersionedKeyTests.cs](../tests/EventSourcing.UxProjections.Abstractions.L0Tests/UxProjectionVersionedKeyTests.cs)

### EventSourcing.UxProjections.Api.L0Tests

- [ ] [TestBrookDefinition.cs](../tests/EventSourcing.UxProjections.Api.L0Tests/TestBrookDefinition.cs)
- [ ] [TestProjection.cs](../tests/EventSourcing.UxProjections.Api.L0Tests/TestProjection.cs)
- [ ] [UxProjectionControllerTests.cs](../tests/EventSourcing.UxProjections.Api.L0Tests/UxProjectionControllerTests.cs)

### EventSourcing.UxProjections.L0Tests

- [ ] [Subscriptions/UxProjectionSubscriptionGrainTests.cs](../tests/EventSourcing.UxProjections.L0Tests/Subscriptions/UxProjectionSubscriptionGrainTests.cs)
- [ ] [TestGrain.cs](../tests/EventSourcing.UxProjections.L0Tests/TestGrain.cs)
- [ ] [TestProjection.cs](../tests/EventSourcing.UxProjections.L0Tests/TestProjection.cs)
- [ ] [UxProjectionCursorGrainIntegrationTests.cs](../tests/EventSourcing.UxProjections.L0Tests/UxProjectionCursorGrainIntegrationTests.cs)
- [ ] [UxProjectionCursorGrainTests.cs](../tests/EventSourcing.UxProjections.L0Tests/UxProjectionCursorGrainTests.cs)
- [ ] [UxProjectionGrainFactoryTests.cs](../tests/EventSourcing.UxProjections.L0Tests/UxProjectionGrainFactoryTests.cs)
- [ ] [UxProjectionGrainTests.cs](../tests/EventSourcing.UxProjections.L0Tests/UxProjectionGrainTests.cs)
- [ ] [UxProjectionVersionedCacheGrainTests.cs](../tests/EventSourcing.UxProjections.L0Tests/UxProjectionVersionedCacheGrainTests.cs)

### EventSourcing.UxProjections.SignalR.L0Tests

- [ ] [Grains/UxClientGrainTests.cs](../tests/EventSourcing.UxProjections.SignalR.L0Tests/Grains/UxClientGrainTests.cs)
- [ ] [Grains/UxGroupGrainTests.cs](../tests/EventSourcing.UxProjections.SignalR.L0Tests/Grains/UxGroupGrainTests.cs)
- [ ] [Grains/UxServerDirectoryGrainTests.cs](../tests/EventSourcing.UxProjections.SignalR.L0Tests/Grains/UxServerDirectoryGrainTests.cs)

---

## Sample Files (samples/)

### Cascade.Domain

- [ ] [CascadeRegistrations.cs](../samples/Cascade/Cascade.Domain/CascadeRegistrations.cs)
- [ ] [Channel/IChannelAggregateGrain.cs](../samples/Cascade/Cascade.Domain/Channel/IChannelAggregateGrain.cs)
- [ ] [Channel/ChannelAggregate.cs](../samples/Cascade/Cascade.Domain/Channel/ChannelAggregate.cs)
- [ ] [Channel/ChannelAggregateGrain.cs](../samples/Cascade/Cascade.Domain/Channel/ChannelAggregateGrain.cs)
- [ ] [Channel/Commands/AddMember.cs](../samples/Cascade/Cascade.Domain/Channel/Commands/AddMember.cs)
- [ ] [Channel/Commands/ArchiveChannel.cs](../samples/Cascade/Cascade.Domain/Channel/Commands/ArchiveChannel.cs)
- [ ] [Channel/Commands/CreateChannel.cs](../samples/Cascade/Cascade.Domain/Channel/Commands/CreateChannel.cs)
- [ ] [Channel/Commands/RemoveMember.cs](../samples/Cascade/Cascade.Domain/Channel/Commands/RemoveMember.cs)
- [ ] [Channel/Commands/RenameChannel.cs](../samples/Cascade/Cascade.Domain/Channel/Commands/RenameChannel.cs)
- [ ] [Channel/Events/ChannelArchived.cs](../samples/Cascade/Cascade.Domain/Channel/Events/ChannelArchived.cs)
- [ ] [Channel/Events/ChannelCreated.cs](../samples/Cascade/Cascade.Domain/Channel/Events/ChannelCreated.cs)
- [ ] [Channel/Events/ChannelRenamed.cs](../samples/Cascade/Cascade.Domain/Channel/Events/ChannelRenamed.cs)
- [ ] [Channel/Events/MemberAdded.cs](../samples/Cascade/Cascade.Domain/Channel/Events/MemberAdded.cs)
- [ ] [Channel/Events/MemberRemoved.cs](../samples/Cascade/Cascade.Domain/Channel/Events/MemberRemoved.cs)
- [ ] [Channel/Handlers/AddMemberHandler.cs](../samples/Cascade/Cascade.Domain/Channel/Handlers/AddMemberHandler.cs)
- [ ] [Channel/Handlers/ArchiveChannelHandler.cs](../samples/Cascade/Cascade.Domain/Channel/Handlers/ArchiveChannelHandler.cs)
- [ ] [Channel/Handlers/CreateChannelHandler.cs](../samples/Cascade/Cascade.Domain/Channel/Handlers/CreateChannelHandler.cs)
- [ ] [Channel/Handlers/RemoveMemberHandler.cs](../samples/Cascade/Cascade.Domain/Channel/Handlers/RemoveMemberHandler.cs)
- [ ] [Channel/Handlers/RenameChannelHandler.cs](../samples/Cascade/Cascade.Domain/Channel/Handlers/RenameChannelHandler.cs)
- [ ] [Channel/Reducers/ChannelArchivedReducer.cs](../samples/Cascade/Cascade.Domain/Channel/Reducers/ChannelArchivedReducer.cs)
- [ ] [Channel/Reducers/ChannelCreatedReducer.cs](../samples/Cascade/Cascade.Domain/Channel/Reducers/ChannelCreatedReducer.cs)
- [ ] [Channel/Reducers/ChannelRenamedReducer.cs](../samples/Cascade/Cascade.Domain/Channel/Reducers/ChannelRenamedReducer.cs)
- [ ] [Channel/Reducers/MemberAddedReducer.cs](../samples/Cascade/Cascade.Domain/Channel/Reducers/MemberAddedReducer.cs)
- [ ] [Channel/Reducers/MemberRemovedReducer.cs](../samples/Cascade/Cascade.Domain/Channel/Reducers/MemberRemovedReducer.cs)
- [ ] [Conversation/IConversationAggregateGrain.cs](../samples/Cascade/Cascade.Domain/Conversation/IConversationAggregateGrain.cs)
- [ ] [Conversation/ConversationAggregate.cs](../samples/Cascade/Cascade.Domain/Conversation/ConversationAggregate.cs)
- [ ] [Conversation/ConversationAggregateGrain.cs](../samples/Cascade/Cascade.Domain/Conversation/ConversationAggregateGrain.cs)
- [ ] [Conversation/Message.cs](../samples/Cascade/Cascade.Domain/Conversation/Message.cs)
- [ ] [Conversation/Commands/DeleteMessage.cs](../samples/Cascade/Cascade.Domain/Conversation/Commands/DeleteMessage.cs)
- [ ] [Conversation/Commands/EditMessage.cs](../samples/Cascade/Cascade.Domain/Conversation/Commands/EditMessage.cs)
- [ ] [Conversation/Commands/SendMessage.cs](../samples/Cascade/Cascade.Domain/Conversation/Commands/SendMessage.cs)
- [ ] [Conversation/Commands/StartConversation.cs](../samples/Cascade/Cascade.Domain/Conversation/Commands/StartConversation.cs)
- [ ] [Conversation/Events/ConversationStarted.cs](../samples/Cascade/Cascade.Domain/Conversation/Events/ConversationStarted.cs)
- [ ] [Conversation/Events/MessageDeleted.cs](../samples/Cascade/Cascade.Domain/Conversation/Events/MessageDeleted.cs)
- [ ] [Conversation/Events/MessageEdited.cs](../samples/Cascade/Cascade.Domain/Conversation/Events/MessageEdited.cs)
- [ ] [Conversation/Events/MessageSent.cs](../samples/Cascade/Cascade.Domain/Conversation/Events/MessageSent.cs)
- [ ] [Conversation/Handlers/DeleteMessageHandler.cs](../samples/Cascade/Cascade.Domain/Conversation/Handlers/DeleteMessageHandler.cs)
- [ ] [Conversation/Handlers/EditMessageHandler.cs](../samples/Cascade/Cascade.Domain/Conversation/Handlers/EditMessageHandler.cs)
- [ ] [Conversation/Handlers/SendMessageHandler.cs](../samples/Cascade/Cascade.Domain/Conversation/Handlers/SendMessageHandler.cs)
- [ ] [Conversation/Handlers/StartConversationHandler.cs](../samples/Cascade/Cascade.Domain/Conversation/Handlers/StartConversationHandler.cs)
- [ ] [Conversation/Reducers/ConversationStartedReducer.cs](../samples/Cascade/Cascade.Domain/Conversation/Reducers/ConversationStartedReducer.cs)
- [ ] [Conversation/Reducers/MessageDeletedReducer.cs](../samples/Cascade/Cascade.Domain/Conversation/Reducers/MessageDeletedReducer.cs)
- [ ] [Conversation/Reducers/MessageEditedReducer.cs](../samples/Cascade/Cascade.Domain/Conversation/Reducers/MessageEditedReducer.cs)
- [ ] [Conversation/Reducers/MessageSentReducer.cs](../samples/Cascade/Cascade.Domain/Conversation/Reducers/MessageSentReducer.cs)
- [ ] [User/IUserAggregateGrain.cs](../samples/Cascade/Cascade.Domain/User/IUserAggregateGrain.cs)
- [ ] [User/UserAggregate.cs](../samples/Cascade/Cascade.Domain/User/UserAggregate.cs)
- [ ] [User/UserAggregateGrain.cs](../samples/Cascade/Cascade.Domain/User/UserAggregateGrain.cs)
- [ ] [User/Commands/JoinChannel.cs](../samples/Cascade/Cascade.Domain/User/Commands/JoinChannel.cs)
- [ ] [User/Commands/LeaveChannel.cs](../samples/Cascade/Cascade.Domain/User/Commands/LeaveChannel.cs)
- [ ] [User/Commands/RegisterUser.cs](../samples/Cascade/Cascade.Domain/User/Commands/RegisterUser.cs)
- [ ] [User/Commands/SetOnlineStatus.cs](../samples/Cascade/Cascade.Domain/User/Commands/SetOnlineStatus.cs)
- [ ] [User/Commands/UpdateDisplayName.cs](../samples/Cascade/Cascade.Domain/User/Commands/UpdateDisplayName.cs)
- [ ] [User/Events/DisplayNameUpdated.cs](../samples/Cascade/Cascade.Domain/User/Events/DisplayNameUpdated.cs)
- [ ] [User/Events/UserJoinedChannel.cs](../samples/Cascade/Cascade.Domain/User/Events/UserJoinedChannel.cs)
- [ ] [User/Events/UserLeftChannel.cs](../samples/Cascade/Cascade.Domain/User/Events/UserLeftChannel.cs)
- [ ] [User/Events/UserRegistered.cs](../samples/Cascade/Cascade.Domain/User/Events/UserRegistered.cs)
- [ ] [User/Events/UserWentOffline.cs](../samples/Cascade/Cascade.Domain/User/Events/UserWentOffline.cs)
- [ ] [User/Events/UserWentOnline.cs](../samples/Cascade/Cascade.Domain/User/Events/UserWentOnline.cs)
- [ ] [User/Handlers/JoinChannelHandler.cs](../samples/Cascade/Cascade.Domain/User/Handlers/JoinChannelHandler.cs)
- [ ] [User/Handlers/LeaveChannelHandler.cs](../samples/Cascade/Cascade.Domain/User/Handlers/LeaveChannelHandler.cs)
- [ ] [User/Handlers/RegisterUserHandler.cs](../samples/Cascade/Cascade.Domain/User/Handlers/RegisterUserHandler.cs)
- [ ] [User/Handlers/SetOnlineStatusHandler.cs](../samples/Cascade/Cascade.Domain/User/Handlers/SetOnlineStatusHandler.cs)
- [ ] [User/Handlers/UpdateDisplayNameHandler.cs](../samples/Cascade/Cascade.Domain/User/Handlers/UpdateDisplayNameHandler.cs)
- [ ] [User/Reducers/DisplayNameUpdatedReducer.cs](../samples/Cascade/Cascade.Domain/User/Reducers/DisplayNameUpdatedReducer.cs)
- [ ] [User/Reducers/UserJoinedChannelReducer.cs](../samples/Cascade/Cascade.Domain/User/Reducers/UserJoinedChannelReducer.cs)
- [ ] [User/Reducers/UserLeftChannelReducer.cs](../samples/Cascade/Cascade.Domain/User/Reducers/UserLeftChannelReducer.cs)
- [ ] [User/Reducers/UserRegisteredReducer.cs](../samples/Cascade/Cascade.Domain/User/Reducers/UserRegisteredReducer.cs)
- [ ] [User/Reducers/UserWentOfflineReducer.cs](../samples/Cascade/Cascade.Domain/User/Reducers/UserWentOfflineReducer.cs)
- [ ] [User/Reducers/UserWentOnlineReducer.cs](../samples/Cascade/Cascade.Domain/User/Reducers/UserWentOnlineReducer.cs)
- [ ] [Projections/UserProfile/UserProfileProjection.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/UserProfileProjection.cs)
- [ ] [Projections/UserProfile/UserProfileProjectionGrain.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/UserProfileProjectionGrain.cs)
- [ ] [Projections/UserProfile/Reducers/DisplayNameUpdatedProjectionReducer.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/Reducers/DisplayNameUpdatedProjectionReducer.cs)
- [ ] [Projections/UserProfile/Reducers/UserJoinedChannelProjectionReducer.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/Reducers/UserJoinedChannelProjectionReducer.cs)
- [ ] [Projections/UserProfile/Reducers/UserLeftChannelProjectionReducer.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/Reducers/UserLeftChannelProjectionReducer.cs)
- [ ] [Projections/UserProfile/Reducers/UserRegisteredProjectionReducer.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/Reducers/UserRegisteredProjectionReducer.cs)
- [ ] [Projections/UserProfile/Reducers/UserWentOfflineProjectionReducer.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/Reducers/UserWentOfflineProjectionReducer.cs)
- [ ] [Projections/UserProfile/Reducers/UserWentOnlineProjectionReducer.cs](../samples/Cascade/Cascade.Domain/Projections/UserProfile/Reducers/UserWentOnlineProjectionReducer.cs)

### Cascade.Server

- [ ] [Components/Services/IChatService.cs](../samples/Cascade/Cascade.Server/Components/Services/IChatService.cs)
- [ ] [Components/Services/IProjectionSubscriber.cs](../samples/Cascade/Cascade.Server/Components/Services/IProjectionSubscriber.cs)
- [ ] [Components/Services/IProjectionSubscriberFactory.cs](../samples/Cascade/Cascade.Server/Components/Services/IProjectionSubscriberFactory.cs)
- [ ] [Components/Services/CascadeServerServiceCollectionExtensions.cs](../samples/Cascade/Cascade.Server/Components/Services/CascadeServerServiceCollectionExtensions.cs)
- [ ] [Components/Services/ChatOperationException.cs](../samples/Cascade/Cascade.Server/Components/Services/ChatOperationException.cs)
- [ ] [Components/Services/ChatService.cs](../samples/Cascade/Cascade.Server/Components/Services/ChatService.cs)
- [ ] [Components/Services/ProjectionErrorEventArgs.cs](../samples/Cascade/Cascade.Server/Components/Services/ProjectionErrorEventArgs.cs)
- [ ] [Components/Services/ProjectionSubscriber.cs](../samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriber.cs)
- [ ] [Components/Services/ProjectionSubscriberFactory.cs](../samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriberFactory.cs)
- [ ] [Components/Services/ProjectionSubscriberLoggerExtensions.cs](../samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriberLoggerExtensions.cs)
- [ ] [Components/Services/UserSession.cs](../samples/Cascade/Cascade.Server/Components/Services/UserSession.cs)
- [ ] [Components/ViewModels/MemberViewModel.cs](../samples/Cascade/Cascade.Server/Components/ViewModels/MemberViewModel.cs)
- [ ] [Components/ViewModels/MessageViewModel.cs](../samples/Cascade/Cascade.Server/Components/ViewModels/MessageViewModel.cs)
- [ ] [Program.cs](../samples/Cascade/Cascade.Server/Program.cs)

### Cascade.AppHost

- [ ] [Program.cs](../samples/Cascade/Cascade.AppHost/Program.cs)

### Cascade.Domain.L0Tests

- [ ] [Channel/Handlers/ArchiveChannelHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Channel/Handlers/ArchiveChannelHandlerTests.cs)
- [ ] [Channel/Handlers/CreateChannelHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Channel/Handlers/CreateChannelHandlerTests.cs)
- [ ] [Channel/Handlers/MemberHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Channel/Handlers/MemberHandlerTests.cs)
- [ ] [Channel/Handlers/RenameChannelHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Channel/Handlers/RenameChannelHandlerTests.cs)
- [ ] [Channel/Reducers/ChannelReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Channel/Reducers/ChannelReducerTests.cs)
- [ ] [Conversation/Handlers/DeleteMessageHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Handlers/DeleteMessageHandlerTests.cs)
- [ ] [Conversation/Handlers/EditMessageHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Handlers/EditMessageHandlerTests.cs)
- [ ] [Conversation/Handlers/SendMessageHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Handlers/SendMessageHandlerTests.cs)
- [ ] [Conversation/Handlers/StartConversationHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Handlers/StartConversationHandlerTests.cs)
- [ ] [Conversation/Reducers/ConversationStartedReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Reducers/ConversationStartedReducerTests.cs)
- [ ] [Conversation/Reducers/MessageDeletedReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Reducers/MessageDeletedReducerTests.cs)
- [ ] [Conversation/Reducers/MessageEditedReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Reducers/MessageEditedReducerTests.cs)
- [ ] [Conversation/Reducers/MessageSentReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Conversation/Reducers/MessageSentReducerTests.cs)
- [ ] [Projections/UserProfile/Reducers/DisplayNameUpdatedProjectionReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Projections/UserProfile/Reducers/DisplayNameUpdatedProjectionReducerTests.cs)
- [ ] [Projections/UserProfile/Reducers/UserJoinedChannelProjectionReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Projections/UserProfile/Reducers/UserJoinedChannelProjectionReducerTests.cs)
- [ ] [Projections/UserProfile/Reducers/UserLeftChannelProjectionReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Projections/UserProfile/Reducers/UserLeftChannelProjectionReducerTests.cs)
- [ ] [Projections/UserProfile/Reducers/UserRegisteredProjectionReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Projections/UserProfile/Reducers/UserRegisteredProjectionReducerTests.cs)
- [ ] [Projections/UserProfile/Reducers/UserWentOfflineProjectionReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Projections/UserProfile/Reducers/UserWentOfflineProjectionReducerTests.cs)
- [ ] [Projections/UserProfile/Reducers/UserWentOnlineProjectionReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/Projections/UserProfile/Reducers/UserWentOnlineProjectionReducerTests.cs)
- [ ] [User/Handlers/JoinChannelHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Handlers/JoinChannelHandlerTests.cs)
- [ ] [User/Handlers/LeaveChannelHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Handlers/LeaveChannelHandlerTests.cs)
- [ ] [User/Handlers/RegisterUserHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Handlers/RegisterUserHandlerTests.cs)
- [ ] [User/Handlers/SetOnlineStatusHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Handlers/SetOnlineStatusHandlerTests.cs)
- [ ] [User/Handlers/UpdateDisplayNameHandlerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Handlers/UpdateDisplayNameHandlerTests.cs)
- [ ] [User/Reducers/ChannelMembershipReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Reducers/ChannelMembershipReducerTests.cs)
- [ ] [User/Reducers/DisplayNameUpdatedReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Reducers/DisplayNameUpdatedReducerTests.cs)
- [ ] [User/Reducers/OnlineStatusReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Reducers/OnlineStatusReducerTests.cs)
- [ ] [User/Reducers/UserRegisteredReducerTests.cs](../samples/Cascade/Cascade.Domain.L0Tests/User/Reducers/UserRegisteredReducerTests.cs)

### Cascade.L2Tests

- [ ] [Features/ChannelCreationTests.cs](../samples/Cascade/Cascade.L2Tests/Features/ChannelCreationTests.cs)
- [ ] [Features/LoginTests.cs](../samples/Cascade/Cascade.L2Tests/Features/LoginTests.cs)
- [ ] [Features/MessagingTests.cs](../samples/Cascade/Cascade.L2Tests/Features/MessagingTests.cs)
- [ ] [Features/RealTimeTests.cs](../samples/Cascade/Cascade.L2Tests/Features/RealTimeTests.cs)
- [ ] [GlobalUsings.cs](../samples/Cascade/Cascade.L2Tests/GlobalUsings.cs)
- [ ] [PageObjects/ChannelListPage.cs](../samples/Cascade/Cascade.L2Tests/PageObjects/ChannelListPage.cs)
- [ ] [PageObjects/ChannelViewPage.cs](../samples/Cascade/Cascade.L2Tests/PageObjects/ChannelViewPage.cs)
- [ ] [PageObjects/LoginPage.cs](../samples/Cascade/Cascade.L2Tests/PageObjects/LoginPage.cs)
- [ ] [PlaywrightFixture.cs](../samples/Cascade/Cascade.L2Tests/PlaywrightFixture.cs)
- [ ] [TestBase.cs](../samples/Cascade/Cascade.L2Tests/TestBase.cs)

### Crescent.ConsoleApp

- [ ] [Counter/BrookNames.cs](../samples/Crescent/ConsoleApp/Counter/BrookNames.cs)
- [ ] [Counter/CounterAggregate.cs](../samples/Crescent/ConsoleApp/Counter/CounterAggregate.cs)
- [ ] [Counter/CounterAggregateGrain.cs](../samples/Crescent/ConsoleApp/Counter/CounterAggregateGrain.cs)
- [ ] [Counter/CounterBrook.cs](../samples/Crescent/ConsoleApp/Counter/CounterBrook.cs)
- [ ] [Counter/CounterRegistrations.cs](../samples/Crescent/ConsoleApp/Counter/CounterRegistrations.cs)
- [ ] [Counter/CounterStateSnapshotCacheGrain.cs](../samples/Crescent/ConsoleApp/Counter/CounterStateSnapshotCacheGrain.cs)
- [ ] [Counter/Events/CounterDecremented.cs](../samples/Crescent/ConsoleApp/Counter/Events/CounterDecremented.cs)
- [ ] [Counter/Events/CounterIncremented.cs](../samples/Crescent/ConsoleApp/Counter/Events/CounterIncremented.cs)
- [ ] [Counter/Events/CounterInitialized.cs](../samples/Crescent/ConsoleApp/Counter/Events/CounterInitialized.cs)
- [ ] [Counter/Events/CounterReset.cs](../samples/Crescent/ConsoleApp/Counter/Events/CounterReset.cs)
- [ ] [Counter/Handlers/DecrementCounterHandler.cs](../samples/Crescent/ConsoleApp/Counter/Handlers/DecrementCounterHandler.cs)
- [ ] [Counter/Handlers/IncrementCounterHandler.cs](../samples/Crescent/ConsoleApp/Counter/Handlers/IncrementCounterHandler.cs)
- [ ] [Counter/Handlers/InitializeCounterHandler.cs](../samples/Crescent/ConsoleApp/Counter/Handlers/InitializeCounterHandler.cs)
- [ ] [Counter/Handlers/ResetCounterHandler.cs](../samples/Crescent/ConsoleApp/Counter/Handlers/ResetCounterHandler.cs)
- [ ] [Counter/Reducers/CounterDecrementedReducer.cs](../samples/Crescent/ConsoleApp/Counter/Reducers/CounterDecrementedReducer.cs)
- [ ] [Counter/Reducers/CounterIncrementedReducer.cs](../samples/Crescent/ConsoleApp/Counter/Reducers/CounterIncrementedReducer.cs)
- [ ] [Counter/Reducers/CounterInitializedReducer.cs](../samples/Crescent/ConsoleApp/Counter/Reducers/CounterInitializedReducer.cs)
- [ ] [Counter/Reducers/CounterResetReducer.cs](../samples/Crescent/ConsoleApp/Counter/Reducers/CounterResetReducer.cs)
- [ ] [CounterSummary/CounterSummaryProjection.cs](../samples/Crescent/ConsoleApp/CounterSummary/CounterSummaryProjection.cs)
- [ ] [CounterSummary/CounterSummaryProjectionGrain.cs](../samples/Crescent/ConsoleApp/CounterSummary/CounterSummaryProjectionGrain.cs)
- [ ] [CounterSummary/CounterSummarySnapshotCacheGrain.cs](../samples/Crescent/ConsoleApp/CounterSummary/CounterSummarySnapshotCacheGrain.cs)
- [ ] [CounterSummary/CounterSummaryVersionedCacheGrain.cs](../samples/Crescent/ConsoleApp/CounterSummary/CounterSummaryVersionedCacheGrain.cs)
- [ ] [Program.cs](../samples/Crescent/ConsoleApp/Program.cs)
- [ ] [Scenarios/AggregateScenario.cs](../samples/Crescent/ConsoleApp/Scenarios/AggregateScenario.cs)
- [ ] [Scenarios/ComprehensiveE2EScenarios.cs](../samples/Crescent/ConsoleApp/Scenarios/ComprehensiveE2EScenarios.cs)
- [ ] [Scenarios/MultiStreamScenario.cs](../samples/Crescent/ConsoleApp/Scenarios/MultiStreamScenario.cs)
- [ ] [Scenarios/SimpleUxProjectionScenario.cs](../samples/Crescent/ConsoleApp/Scenarios/SimpleUxProjectionScenario.cs)
- [ ] [Scenarios/UxProjectionScenario.cs](../samples/Crescent/ConsoleApp/Scenarios/UxProjectionScenario.cs)
- [ ] [Scenarios/VerificationScenario.cs](../samples/Crescent/ConsoleApp/Scenarios/VerificationScenario.cs)

### Crescent.NewModel

- [ ] [Chat/IChatAggregateGrain.cs](../samples/Crescent/NewModel/Chat/IChatAggregateGrain.cs)
- [ ] [Chat/ChatAggregate.cs](../samples/Crescent/NewModel/Chat/ChatAggregate.cs)
- [ ] [Chat/ChatAggregateGrain.cs](../samples/Crescent/NewModel/Chat/ChatAggregateGrain.cs)
- [ ] [Chat/ChatMessage.cs](../samples/Crescent/NewModel/Chat/ChatMessage.cs)
- [ ] [Chat/ChatRegistrations.cs](../samples/Crescent/NewModel/Chat/ChatRegistrations.cs)
- [ ] [Chat/Commands/AddMessage.cs](../samples/Crescent/NewModel/Chat/Commands/AddMessage.cs)
- [ ] [Chat/Commands/CreateChat.cs](../samples/Crescent/NewModel/Chat/Commands/CreateChat.cs)
- [ ] [Chat/Commands/DeleteMessage.cs](../samples/Crescent/NewModel/Chat/Commands/DeleteMessage.cs)
- [ ] [Chat/Commands/EditMessage.cs](../samples/Crescent/NewModel/Chat/Commands/EditMessage.cs)
- [ ] [Chat/Events/ChatCreated.cs](../samples/Crescent/NewModel/Chat/Events/ChatCreated.cs)
- [ ] [Chat/Events/MessageAdded.cs](../samples/Crescent/NewModel/Chat/Events/MessageAdded.cs)
- [ ] [Chat/Events/MessageDeleted.cs](../samples/Crescent/NewModel/Chat/Events/MessageDeleted.cs)
- [ ] [Chat/Events/MessageEdited.cs](../samples/Crescent/NewModel/Chat/Events/MessageEdited.cs)
- [ ] [Chat/Handlers/AddMessageHandler.cs](../samples/Crescent/NewModel/Chat/Handlers/AddMessageHandler.cs)
- [ ] [Chat/Handlers/CreateChatHandler.cs](../samples/Crescent/NewModel/Chat/Handlers/CreateChatHandler.cs)
- [ ] [Chat/Handlers/DeleteMessageHandler.cs](../samples/Crescent/NewModel/Chat/Handlers/DeleteMessageHandler.cs)
- [ ] [Chat/Handlers/EditMessageHandler.cs](../samples/Crescent/NewModel/Chat/Handlers/EditMessageHandler.cs)
- [ ] [Chat/Reducers/ChatCreatedReducer.cs](../samples/Crescent/NewModel/Chat/Reducers/ChatCreatedReducer.cs)
- [ ] [Chat/Reducers/MessageAddedReducer.cs](../samples/Crescent/NewModel/Chat/Reducers/MessageAddedReducer.cs)
- [ ] [Chat/Reducers/MessageDeletedReducer.cs](../samples/Crescent/NewModel/Chat/Reducers/MessageDeletedReducer.cs)
- [ ] [Chat/Reducers/MessageEditedReducer.cs](../samples/Crescent/NewModel/Chat/Reducers/MessageEditedReducer.cs)
- [ ] [ChatSummary/ChatSummaryProjection.cs](../samples/Crescent/NewModel/ChatSummary/ChatSummaryProjection.cs)
- [ ] [ChatSummary/ChatSummaryProjectionGrain.cs](../samples/Crescent/NewModel/ChatSummary/ChatSummaryProjectionGrain.cs)
- [ ] [ChatSummary/ChatSummaryRegistrations.cs](../samples/Crescent/NewModel/ChatSummary/ChatSummaryRegistrations.cs)
- [ ] [ChatSummary/Reducers/ChatSummaryCreatedReducer.cs](../samples/Crescent/NewModel/ChatSummary/Reducers/ChatSummaryCreatedReducer.cs)
- [ ] [ChatSummary/Reducers/ChatSummaryMessageAddedReducer.cs](../samples/Crescent/NewModel/ChatSummary/Reducers/ChatSummaryMessageAddedReducer.cs)
- [ ] [ChatSummary/Reducers/ChatSummaryMessageDeletedReducer.cs](../samples/Crescent/NewModel/ChatSummary/Reducers/ChatSummaryMessageDeletedReducer.cs)
- [ ] [ChatSummary/Reducers/ChatSummaryMessageEditedReducer.cs](../samples/Crescent/NewModel/ChatSummary/Reducers/ChatSummaryMessageEditedReducer.cs)

### Crescent.NewModel.L0Tests

- [ ] [Chat/Handlers/AddMessageHandlerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Handlers/AddMessageHandlerTests.cs)
- [ ] [Chat/Handlers/CreateChatHandlerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Handlers/CreateChatHandlerTests.cs)
- [ ] [Chat/Handlers/DeleteMessageHandlerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Handlers/DeleteMessageHandlerTests.cs)
- [ ] [Chat/Handlers/EditMessageHandlerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Handlers/EditMessageHandlerTests.cs)
- [ ] [Chat/Reducers/ChatCreatedReducerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Reducers/ChatCreatedReducerTests.cs)
- [ ] [Chat/Reducers/MessageAddedReducerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Reducers/MessageAddedReducerTests.cs)
- [ ] [Chat/Reducers/MessageDeletedReducerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Reducers/MessageDeletedReducerTests.cs)
- [ ] [Chat/Reducers/MessageEditedReducerTests.cs](../samples/Crescent/NewModel.L0Tests/Chat/Reducers/MessageEditedReducerTests.cs)

---

## Statistics

- **Total C# Files Changed**: 257
- **Framework Source (src/)**: 64
- **Framework Tests (tests/)**: 47
- **Sample Files (samples/)**: 146
