using System.Text.Json.Serialization;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.API;

[JsonSerializable(typeof(Movie))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}