using System.Collections.Immutable;


namespace Mississippi.Core.Idea;

public interface IEventSourcingWriteProvider
{
    Task AppendEvent<T>(
        string streamId,
        T @event,
        long expectedVersion
    );

    Task AppendEvents(
        string streamId,
        ImmutableArray<object> @events,
        long expectedVersion
    );
}

public interface IEventSourcingReadProvider
{
    IAsyncEnumerable<object> ReadEventsAsync(
        string streamId,
        long from,
        long to,
        CancellationToken token
    );
}

public interface IMississippiEventConverter
{
    Task<object> Convert(
        MississippiEvent @event
    );
}


public class TestBaseProjection
{

    Task Married()
    {
        // raise event that the user is married on the base stream.
        // get base stream type from attribute
        // get id from the grain id.

        // Call a service which takes the event and saves it.
        // that service should
        // deserilize the event
        // create a missippi event
        // pass that to the stream grain, that then saves it.
    }

    Task Read()
    {
        // we should have a projection
        // the projetion should

        // we should have a grain which

    }
}



// [QueryStream-Type-Id]
// [EventStream-Type-Id]
// IQueryGrain
