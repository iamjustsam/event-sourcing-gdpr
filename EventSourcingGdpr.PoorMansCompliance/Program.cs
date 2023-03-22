// See https://aka.ms/new-console-template for more information

using Marten;
using Serilog;
using Weasel.Core;
using DataRemovalRequested = EventSourcingGdpr.PoorMansCompliance.Domain.DataRemovalRequested;
using Person = EventSourcingGdpr.PoorMansCompliance.Domain.Person;
using PersonCreated = EventSourcingGdpr.PoorMansCompliance.Domain.PersonCreated;
using PersonEmailUpdated = EventSourcingGdpr.PoorMansCompliance.Domain.PersonEmailUpdated;

await using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var store = DocumentStore.For(options =>
{
    options.Connection("Server=localhost;Database=event-sourcing-gdpr;Port=5432;User Id=postgres;Password=postgres;");
    options.DatabaseSchemaName = "PoorMansCompliance";
    options.AutoCreateSchemaObjects = AutoCreate.All;

    options.Events.AddEventType<PersonCreated>();
    options.Events.AddEventType<PersonEmailUpdated>();
    options.Events.AddEventType<DataRemovalRequested>();

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

// Append data removal requested event
var personToUpdate = await session.Events.AggregateStreamAsync<Person>(person.Id);
personToUpdate!.HandleDataRemovalRequest();
session.Events.Append(personToUpdate.Id, personToUpdate.DomainEvents);

// Save updated stream
await session.SaveChangesAsync();

// Eject previously fetched data
session.Eject(dbPerson!);

dbPerson = await session.LoadAsync<Person>(person.Id);
log.Information("{@Person}", dbPerson);
