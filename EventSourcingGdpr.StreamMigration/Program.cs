using EventSourcingGdpr.Cryptoshredding.Domain;
using EventSourcingGdpr.StreamMigration;
using Marten;
using Serilog;
using Weasel.Core;

await using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var store = DocumentStore.For(options =>
{
    options.Connection("Server=localhost;Database=event-sourcing-gdpr;Port=5432;User Id=postgres;Password=postgres;");
    options.DatabaseSchemaName = "StreamMigration";
    options.AutoCreateSchemaObjects = AutoCreate.All;

    options.Events.AddEventType<PersonCreated>();
    options.Events.AddEventType<PersonEmailUpdated>();

    options.Projections.SelfAggregate<Person>();
});

await using var session = store.OpenSession();

var person = Person.CreatePerson(Guid.NewGuid(), "Dina", "Van Ordina");
person.UpdateEmail("dina@ordina.be");
session.Events.Append(person.Id, person.DomainEvents);

await session.SaveChangesAsync();

// Fetch and show person
var dbPerson = await session.LoadAsync<Person>(person.Id);
log.Information("{@Person}", dbPerson);

// Load events
var events = await session.Events.FetchStreamAsync(person.Id);

// Transform events
var newEvents = events.Select(@event => @event.Data switch
{
    PersonEmailUpdated eventData => eventData with { Email = "***" },
    _ => @event.Data
});

// Delete the old event stream
await session.DeleteEventStream(person.Id);

// Save new stream
session.Events.Append(person.Id, newEvents);

await session.SaveChangesAsync();

// Eject previously fetched data
session.Eject(dbPerson!);

dbPerson = await session.LoadAsync<Person>(person.Id);
log.Information("{@Person}", dbPerson);
