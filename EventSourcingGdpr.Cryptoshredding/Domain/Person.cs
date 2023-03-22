using EventSourcingGdpr.Cryptoshredding.Cryptoshredding;

namespace EventSourcingGdpr.Cryptoshredding.Domain;

public class Person : Aggregate
{
    [DataSubjectId]
    public Guid Id { get; set; }
    public string Firstname { get; set; } = default!;
    public string Lastname { get; set; } = default!;
    [PersonalData]
    public string? Email { get; set; }

    private Person()
    {
        On<PersonCreated>(Apply);
        On<PersonEmailUpdated>(Apply);
    }

    public static Person CreatePerson(Guid id, string firstName, string lastName)
    {
        var @event = new PersonCreated(id, firstName, lastName);

        var person = new Person();
        person.Handle(@event);

        return person;
    }

    public void UpdateEmail(string email)
    {
        var @event = new PersonEmailUpdated(Id, email);
        Handle(@event);
    }

    public void Apply(PersonCreated @event)
    {
        Id = @event.Id;
        Firstname = @event.FirstName;
        Lastname = @event.LastName;
    }

    public void Apply(PersonEmailUpdated @event)
    {
        Email = @event.Email;
    }
}

public record PersonCreated([property:DataSubjectId] Guid Id, string FirstName, string LastName) : IDomainEvent;
public record PersonEmailUpdated([property:DataSubjectId] Guid Id, [property:PersonalData] string Email) : IDomainEvent;
