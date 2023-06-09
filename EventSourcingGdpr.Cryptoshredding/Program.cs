﻿using EventSourcingGdpr.Cryptoshredding.Cryptoshredding;
using EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Keystore;
using EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Serialization;
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

// Fetch person
var personAggregate = await session.Events.AggregateStreamAsync<Person>(person.Id);
log.Information("{@Person}", personAggregate);

// Append removal request and remove key
personAggregate!.HandleDataRemovalRequest();
session.Events.Append(personAggregate.Id, personAggregate.DomainEvents);
await session.SaveChangesAsync();

var cryptoRepository = scopedProvider.GetRequiredService<ICryptoRepository>();
await cryptoRepository.DeleteEncryptionKey($"{person.Id}", default); // Person.Id is the dataSubjectId, which is used as the key id

// Fetch person again
personAggregate = await session.Events.AggregateStreamAsync<Person>(person.Id);
log.Information("{@Person}", personAggregate);
