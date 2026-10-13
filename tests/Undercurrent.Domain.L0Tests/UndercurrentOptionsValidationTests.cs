using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Undercurrent.Abstractions;
using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications;
using Mississippi.Undercurrent.Domain;


namespace MississippiTests.Undercurrent.Domain.L0Tests;

/// <summary>Verifies real options binding and startup validation for host notification policy.</summary>
public sealed class UndercurrentOptionsValidationTests
{
    /// <summary>Configuration sections bind nested notification settings and preserve other defaults.</summary>
    [Fact]
    public void ConfigurationBindsNotificationPolicy()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Undercurrent:Notifications:MaxNotifications"] = "3",
                    ["Undercurrent:Notifications:MaxCreationReceipts"] = "8",
                    ["Undercurrent:Notifications:MaxRequestLifetime"] = "2.00:00:00",
                    ["Undercurrent:Notifications:MaxTitleLength"] = "80",
                })
            .Build();
        ServiceCollection services = new();
        Assert.Same(services, services.AddUndercurrentDomain(configuration.GetSection("Undercurrent")));
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        UserNotificationsOptions options =
            provider.GetRequiredService<IOptions<UndercurrentOptions>>().Value.Notifications;
        Assert.Equal(3, options.MaxNotifications);
        Assert.Equal(8, options.MaxCreationReceipts);
        Assert.Equal(TimeSpan.FromDays(2), options.MaxRequestLifetime);
        Assert.Equal(80, options.MaxTitleLength);
        Assert.Equal(2_000, options.MaxDescriptionLength);
    }

    /// <summary>Explicit capacities use the same validated registration path.</summary>
    [Fact]
    public void ExplicitCapacitiesConfigureThePolicy()
    {
        ServiceCollection services = new();
        services.AddUndercurrentDomain(4, 12, TimeSpan.FromHours(12));
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();
        UserNotificationsOptions options =
            provider.GetRequiredService<IOptions<UndercurrentOptions>>().Value.Notifications;
        Assert.Equal(4, options.MaxNotifications);
        Assert.Equal(12, options.MaxCreationReceipts);
        Assert.Equal(TimeSpan.FromHours(12), options.MaxRequestLifetime);
    }

    /// <summary>Invalid capacities fail before a host can accept aggregate commands.</summary>
    /// <param name="notifications">The configured notification capacity.</param>
    /// <param name="receipts">The configured receipt capacity.</param>
    [Theory]
    [InlineData(0, 10_000)]
    [InlineData(-1, 10_000)]
    [InlineData(-2, -1)]
    [InlineData(10_001, 20_000)]
    [InlineData(1_000, 1_000)]
    [InlineData(1_000, 0)]
    [InlineData(1_000, 100_001)]
    public void InvalidCapacitiesFailAtStartup(
        int notifications,
        int receipts
    )
    {
        ServiceCollection services = new();
        services.AddUndercurrentDomain(notifications, receipts, TimeSpan.FromDays(7));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    /// <summary>Nonpositive lifetimes, absent policy, and invalid length budgets are rejected.</summary>
    [Fact]
    public void InvalidPolicyValuesAreRejected()
    {
        UndercurrentOptionsValidator validator = new();
        Assert.True(validator.Validate(null, null!).Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = null!,
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxRequestLifetime = TimeSpan.Zero,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxRequestLifetime = TimeSpan.FromTicks(-1),
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxTitleLength = 0,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxCreationKeyLength = 0,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxDescriptionLength = -1,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxIconKeyLength = -1,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxNavigationTargetLength = -1,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxDescriptionLength = int.MaxValue,
                        },
                    })
                .Failed);
        Assert.True(
            validator.Validate(
                    null,
                    new()
                    {
                        Notifications = new()
                        {
                            MaxTitleLength = int.MaxValue,
                            MaxDescriptionLength = int.MaxValue,
                        },
                    })
                .Failed);
    }

    /// <summary>Options registration rejects missing configuration callbacks, sections, and collections.</summary>
    [Fact]
    public void NullRegistrationArgumentsAreRejected()
    {
        ServiceCollection services = new();
        Assert.Throws<ArgumentNullException>(() => services.AddUndercurrentDomain((Action<UndercurrentOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddUndercurrentDomain((IConfiguration)null!));
        Assert.Throws<ArgumentNullException>(() =>
            UndercurrentDomainRegistrations.AddUndercurrentDomain(null!, static _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            UndercurrentDomainRegistrations.AddUndercurrentDomain(null!, new ConfigurationBuilder().Build()));
        Assert.Throws<ArgumentNullException>(() =>
            UndercurrentDomainRegistrations.AddUndercurrentDomain(null!, 1, 2, TimeSpan.FromDays(1)));
    }

    /// <summary>The supported character budget includes its exact upper boundary and permits disabled optional fields.</summary>
    [Fact]
    public void SupportedBoundaryAndOptionalZeroLimitsAreAccepted()
    {
        UndercurrentOptions options = new()
        {
            Notifications = new()
            {
                MaxNotifications = 10_000,
                MaxCreationReceipts = 100_000,
                MaxTitleLength = 1,
                MaxCreationKeyLength = 1,
                MaxDescriptionLength = 16_382,
                MaxIconKeyLength = 0,
                MaxNavigationTargetLength = 0,
            },
        };
        Assert.True(new UndercurrentOptionsValidator().Validate(null, options).Succeeded);
        options.Notifications.MaxDescriptionLength++;
        Assert.True(new UndercurrentOptionsValidator().Validate(null, options).Failed);
    }
}