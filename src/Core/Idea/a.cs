using System.Collections.Immutable;

using Orleans.Streams;


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

// [QueryStream-Type-Id] - This is a single entiy like usersummery
// [EventStream-Type-Id] - this is a single stream like user.

// NS: QueryStream, id: path/id { user/1234 }
// NS: EventStream, id: path/id { user/1234 }

// Query Grain [latest version]
// Query Snapshot [data version]

public class Push
{
    private IStreamProvider StreamProvider { get; }

    Task Update()
    {
        StreamProvider.GetStream<string>("", "");
    }
}

public abstract record ChangedBase
{
    long Version { get; }

    public string Namespace { get; }

    protected abstract string GetPath();

    protected abstract string GetStreamNamespace();
}

public record EventstoreChanged
{
}

public record QueryChanged
{
}

public class EventStore : IStreamNamespacePredicate
{
    public bool IsMatch(
        string streamNamespace
    )
    {
        return streamNamespace.Contains("EventStore");
    }

    public string PredicatePattern { get; }
}

// register of paths
public sealed class PathRegister
{
    private ImmutableDictionary<string, Type> Register { get; set; } = ImmutableDictionary<string, Type>.Empty;

    Type GetType(
        string path
    )
    {
        // extra path here
        if (Register.ContainsKey(path))
        {
            return Register[path];
        }

        throw new Exception("Type not suppored");
    }
}

// rules a query shuold have no logic it shuold all be a reducer.

public sealed class QueryGrain
{
}

public sealed class QueryGrain<TModel>
{
}

public class SampleController
{
    Task GetQuery(
        string path
    )
    {
        // decode
        // get service/entity type
        // get the type T
        // get quert grain of type T
    }
}