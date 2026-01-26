using System.Threading;
using System.Threading.Tasks;


namespace Mississippi.EventSourcing.Aggregates.Abstractions;

/// <summary>
///     Base class for fire-and-forget event effects.
/// </summary>
/// <typeparam name="TEvent">The event type this effect handles.</typeparam>
/// <typeparam name="TAggregate">The aggregate state type.</typeparam>
/// <remarks>
///     <para>
///         Inherit from this class to create fire-and-forget effects. Effects are resolved
///         via DI and can inject any required services (HTTP clients, other grain factories,
///         message publishers, etc.).
///     </para>
///     <para>
///         Effects run in a worker grain that provides Orleans single-threaded guarantees
///         but otherwise is just infrastructure. The effect class itself is where all
///         business logic lives and should be easily unit-testable.
///     </para>
///     <para>
///         Fire-and-forget effects CANNOT yield additional events. If the effect needs to
///         trigger further state changes, it should send commands through the normal
///         aggregate command API.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         public sealed class SendShippingNotificationEffect
///             : FireAndForgetEventEffectBase&lt;OrderShipped, OrderAggregate&gt;
///         {
///             private IEmailService EmailService { get; }
///
///             public SendShippingNotificationEffect(IEmailService emailService)
///             {
///                 EmailService = emailService;
///             }
///
///             public override async Task HandleAsync(
///                 OrderShipped eventData,
///                 OrderAggregate currentState,
///                 CancellationToken cancellationToken)
///             {
///                 await EmailService.SendShippingNotificationAsync(
///                     currentState.CustomerEmail,
///                     eventData.TrackingNumber,
///                     cancellationToken);
///             }
///         }
///     </code>
/// </example>
public abstract class FireAndForgetEventEffectBase<TEvent, TAggregate> : IFireAndForgetEventEffect<TEvent, TAggregate>
    where TEvent : class
    where TAggregate : class
{
    /// <summary>
    ///     Gets a reusable completed task for effects that finish synchronously.
    /// </summary>
    protected static Task CompletedTask => Task.CompletedTask;

    /// <inheritdoc />
    public abstract Task HandleAsync(
        TEvent eventData,
        TAggregate aggregateState,
        string brookKey,
        long eventPosition,
        CancellationToken cancellationToken
    );
}