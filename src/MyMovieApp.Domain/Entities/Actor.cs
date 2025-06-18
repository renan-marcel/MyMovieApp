using System.Text.Json.Serialization;

namespace MyMovieApp.Domain.Entities;

public class Actor
{
    internal Actor(string name)
    {
        // Business rule: validate actor name
        ArgumentException.ThrowIfNullOrEmpty(name, name);
        Id = Guid.NewGuid();
        Name = name;
    }

    [JsonIgnore] public Guid Id { get; private set; }

    public string Name { get; private set; }

    public static Actor Create(string Name)
    {
        // Business rule: validate actor name
        ArgumentException.ThrowIfNullOrEmpty(Name, nameof(Name));

        return new Actor(Name);
    }
}