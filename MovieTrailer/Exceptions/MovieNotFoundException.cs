namespace MovieTrailer.Exceptions;

public class MovieNotFoundException(int id, string mediaType)
    : Exception($"{mediaType} with id {id} was not found.")
{
    public int Id { get; } = id;
    public string MediaType { get; } = mediaType;
}
