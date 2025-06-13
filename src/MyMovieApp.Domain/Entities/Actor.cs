using System.Text.Json.Serialization;

namespace MyMovieApp.Domain.Entities
{
    public class Actor
    {
        [JsonIgnore]
        public Guid Id { get; private set; }

        public string Name { get; private set; }

        internal Actor(string name)
        {
            // Business rule: validate actor name
            ArgumentException.ThrowIfNullOrEmpty(nameof(name), name);
            Id = Guid.NewGuid();
            Name = name;
        }
    }
}
