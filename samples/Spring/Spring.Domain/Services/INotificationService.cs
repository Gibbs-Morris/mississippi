using System.Threading;
using System.Threading.Tasks;


namespace Spring.Domain.Services;

/// <summary>
///     Service for sending customer notifications.
/// </summary>
/// <remarks>
///     <para>
///         This interface abstracts the notification delivery mechanism (email, SMS, push).
///         Effects inject this interface to send notifications without coupling to
///         specific delivery infrastructure.
///     </para>
///     <para>
///         In production, implementations might use SendGrid, Twilio, or Azure Communication Services.
///         For testing, a mock or in-memory implementation can verify notification parameters.
///     </para>
/// </remarks>
public interface INotificationService
{
    /// <summary>
    ///     Sends a withdrawal alert notification to the account holder.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="withdrawalAmount">The amount withdrawn.</param>
    /// <param name="remainingBalance">The balance after withdrawal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendWithdrawalAlertAsync(
        string accountId,
        decimal withdrawalAmount,
        decimal remainingBalance,
        CancellationToken cancellationToken
    );
}