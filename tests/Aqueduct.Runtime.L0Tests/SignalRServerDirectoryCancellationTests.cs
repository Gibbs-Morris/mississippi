using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Aqueduct.Runtime.Grains;

using NSubstitute;

using Orleans.Runtime;


namespace Mississippi.Aqueduct.Runtime.L0Tests;

/// <summary>
///     Verifies cancellation at the concrete directory grain's state mutation boundary.
/// </summary>
public sealed class SignalRServerDirectoryCancellationTests
{
    /// <summary>
    ///     A canceled registration must not create or refresh a server entry.
    /// </summary>
    /// <param name="alreadyRegistered">Whether the directory already contains the server.</param>
    /// <returns>A task representing the test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanceledRegistrationShouldLeaveDirectoryUnchanged(
        bool alreadyRegistered
    )
    {
        FakeTimeProvider time = new();
        SignalRServerDirectoryGrain grain = new(
            Substitute.For<IGrainContext>(),
            NullLogger<SignalRServerDirectoryGrain>.Instance,
            time);
        if (alreadyRegistered)
        {
            await grain.RegisterServerAsync("server");
        }

        time.Advance(TimeSpan.FromMinutes(10));
        CancellationToken canceledToken = new(true);
        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() =>
        {
            _ = grain.RegisterServerAsync("server", canceledToken);
        });
        Assert.Equal(canceledToken, exception.CancellationToken);
        if (alreadyRegistered)
        {
            Assert.Equal("server", Assert.Single(await grain.GetDeadServersAsync(TimeSpan.FromMinutes(5))));
        }
        else
        {
            time.Advance(TimeSpan.FromMinutes(10));
            Assert.Empty(await grain.GetDeadServersAsync(TimeSpan.FromMinutes(5)));
        }
    }
}