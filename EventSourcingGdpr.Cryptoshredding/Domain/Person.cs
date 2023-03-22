using EventSourcingGdpr.Cryptoshredding.Cryptoshredding.Attributes;

namespace EventSourcingGdpr.Cryptoshredding.Domain;

public class Person : Aggregate
{
    public Guid Id { get; set; }
    public string Firstname { get; set; } = default!;
    public string Lastname { get; set; } = default!;
    public string? Email { get; set; }
    public DateTime CreatedDate { get; set; } = default!;

    private Person()
    {
        On<PersonCreated>(Apply);
        On<PersonEmailUpdated>(Apply);
        On<DataRemovalRequested>(Apply);
    }

    public static Person CreatePerson(Guid id, string firstName, string lastName)
    {
        var @event = new PersonCreated(id, firstName, lastName, DateTime.Today);

        var person = new Person();
        person.Handle(@event);

        return person;
    }

    public void UpdateEmail(string email)
    {
        var @event = new PersonEmailUpdated(Id, email);
        Handle(@event);
    }

    public void HandleDataRemovalRequest()
    {
        var @event = new DataRemovalRequested(Id);
        Handle(@event);
    }

    public void Apply(PersonCreated @event)
    {
        Id = @event.Id;
        Firstname = @event.FirstName;
        Lastname = @event.LastName;
        CreatedDate = @event.CreatedDate;
    }

    public void Apply(PersonEmailUpdated @event)
    {
        Email = @event.Email;
    }

    public void Apply(DataRemovalRequested @event)
    {
        Email = "***";
    }
}

public record PersonCreated([property:DataSubjectId] Guid Id, [property:PersonalData] string FirstName, [property:PersonalData] string LastName, DateTime CreatedDate) : IDomainEvent;
public record PersonEmailUpdated([property:DataSubjectId] Guid Id, [property:PersonalData] string Email) : IDomainEvent;
public record DataRemovalRequested(Guid Id) : IDomainEvent;
