using EventSourcingGdpr.Cryptoshredding.Cryptoshredding;
using EventSourcingGdpr.Cryptoshredding.Domain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Weasel.Core;

await using var log = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var services = new ServiceCollection();

services.AddScoped<ICryptoRepository, CryptoRepository>();
services.AddScoped<IEncryptorDecryptor, EncryptorDecryptor>();
services.AddMartenStore<IKeyStore>(options =>
{
    options.Connection("Server=localhost;Database=event-sourcing-gdpr;Port=5432;User Id=postgres;Password=postgres;");
    options.DatabaseSchemaName = "keystore";
});

services.AddMarten(svc =>
{
    var options = new StoreOptions();
    options.Serializer(new EncryptionSerializer(svc.GetRequiredService<IEncryptorDecryptor>()));
    options.Connection("Server=localhost;Database=event-sourcing-gdpr;Port=5432;User Id=postgres;Password=postgres;");
    options.DatabaseSchemaName = "CryptoShredding";
    options.AutoCreateSchemaObjects = AutoCreate.All;

    options.Events.AddEventType<PersonCreated>();
    options.Events.AddEventType<PersonEmailUpdated>();

    options.Projections.SelfAggregate<Person>();

    return options;
});

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var scopedProvider = scope.ServiceProvider;
var session = scopedProvider.GetRequiredService<IDocumentSession>();

var person = Person.CreatePerson(Guid.NewGuid(), "Dina", "Van Ordina");
person.UpdateEmail("dina@ordina.be");
session.Events.Append(person.Id, person.DomainEvents);

await session.SaveChangesAsync();

// Fetch and show person
var dbPerson = await session.LoadAsync<Person>(person.Id);
log.Information("{@Person}", dbPerson);

// Remove key
// TODO: Remove key!

// Eject previously fetched data
session.Eject(dbPerson!);

dbPerson = await session.LoadAsync<Person>(person.Id);
log.Information("{@Person}", dbPerson);
