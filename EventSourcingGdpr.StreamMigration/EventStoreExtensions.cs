using Marten;
using NpgsqlTypes;
using Weasel.Postgresql;

namespace EventSourcingGdpr.StreamMigration;

public static class EventStoreExtensions
{
    public static async Task DeleteEventStream(this IDocumentSession session, Guid streamId)
    {
        var cmdBuilder = new CommandBuilder();
        var parameters = cmdBuilder.AppendWithParameters("delete from streammigration.mt_streams where id = ?; delete from streammigration.mt_events where stream_id = ?");
        parameters[0].NpgsqlDbType = NpgsqlDbType.Uuid;
        parameters[0].Value = streamId;

        parameters[1].NpgsqlDbType = NpgsqlDbType.Uuid;
        parameters[1].Value = streamId;

        await session.ExecuteAsync(cmdBuilder.Compile());
    }
}
