using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Spring.Domain.Services;


namespace Spring.Silo.Services;

/// <summary>
///     Stub implementation of <see cref="INotificationService" /> for demonstration.
/// </summary>
/// <remarks>
///     <para>
///         This stub logs notifications instead of sending them. In production, this would
///         be replaced with an implementation that uses SendGrid, Twilio, Azure Communication
///         Services, or another notification provider.
///     </para>
///     <para>
///         The stub demonstrates that fire-and-forget effects work correctly by verifying
///         the effect is invoked after withdrawals without blocking the command response.
///     </para>
/// </remarks>
internal sealed class StubNotificationService : INotificationService
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StubNotificationService" /> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public StubNotificationService(
        ILogger<StubNotificationService> logger
    ) =>
        Logger = logger;

    private ILogger<StubNotificationService> Logger { get; }

    /// <inheritdoc />
    public Task SendWithdrawalAlertAsync(
        string accountId,
        decimal withdrawalAmount,
        decimal remainingBalance,
        CancellationToken cancellationToken
    )
    {
        Logger.LogWithdrawalNotification(accountId, withdrawalAmount, remainingBalance);

        // In production: await _emailClient.SendAsync(...) or _smsClient.SendAsync(...)
        return Task.CompletedTask;
    }
}