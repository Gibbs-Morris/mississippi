using System;
using System.Linq;

using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;

using Orleans;


namespace MississippiTests.Undercurrent.Abstractions.L0Tests;

/// <summary>Verifies deliberate public access and serialization metadata for direct aggregate callers.</summary>
public sealed class NotificationContractTests
{
    /// <summary>External callers can construct every command and close the generic aggregate contract.</summary>
    [Fact]
    public void CallerContractsArePublicAndExplicitlySerializable()
    {
        Type[] contracts =
        [
            typeof(UserNotificationsAggregate), typeof(UserNotification), typeof(NotificationCreationReceipt),
            typeof(CreateNotification), typeof(MarkNotificationAsRead), typeof(DeleteNotification),
            typeof(DeleteNotificationsBefore),
        ];
        foreach (Type contract in contracts)
        {
            Assert.True(contract.IsPublic);
            Assert.Equal("Mississippi.Undercurrent.Abstractions", contract.Assembly.GetName().Name);
            Assert.NotNull(Attribute.GetCustomAttribute(contract, typeof(GenerateSerializerAttribute)));
            AliasAttribute alias =
                Assert.IsType<AliasAttribute>(Attribute.GetCustomAttribute(contract, typeof(AliasAttribute)));
            Assert.Equal(contract.FullName, alias.Alias);
            uint[] ids = contract.GetProperties()
                .Select(property =>
                    Assert.IsType<IdAttribute>(Attribute.GetCustomAttribute(property, typeof(IdAttribute))).Id)
                .Order()
                .ToArray();
            Assert.Equal(Enumerable.Range(0, ids.Length).Select(index => (uint)index).ToArray(), ids);
        }

        CreateNotification request = new()
        {
            CreationKey = "business-event:notice",
            ExpiresAt = new(2026, 10, 20, 0, 0, 0, TimeSpan.Zero),
            Title = "Ready",
            NavigationTarget = "/deployments/42",
        };
        Assert.Equal("/deployments/42", request.NavigationTarget);
        Assert.Empty(new UserNotificationsAggregate().Notifications);
        Assert.Empty(new UserNotificationsAggregate().NotificationOrder);
        Assert.Empty(new UserNotificationsAggregate().CreationReceipts);
    }
}