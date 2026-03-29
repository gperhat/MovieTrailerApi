using System.ComponentModel.DataAnnotations;

namespace MovieTrailer.Options;

public class TmdbOptions
{
    public const string Section = "Tmdb";

    [Required]
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.themoviedb.org/3";
    public string ImageBaseUrl { get; init; } = "https://image.tmdb.org/t/p";
    public int TimeoutSeconds { get; init; } = 6;
    public int SearchTtlSeconds { get; init; } = 300;
    public int DetailTtlSeconds { get; init; } = 3600;
    public string[] SupportedLanguages { get; init; } = ["en-US"];
}
