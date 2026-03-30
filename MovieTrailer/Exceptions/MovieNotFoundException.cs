
using MovieTrailer.Options;

namespace MovieTrailer.Exceptions;

public class MovieNotFoundException(int id, MediaType mediaType)
    : Exception($"{mediaType} with id {id} was not found.")
{
    public int Id { get; } = id;
    public MediaType MediaType { get; } = mediaType;
}
