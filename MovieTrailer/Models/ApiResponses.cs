namespace MovieTrailer.Models;

public record MovieSearchResultsPage(
    int Page,
    int TotalPages,
    int TotalResults,
    IEnumerable<MovieSummary> Items
);

public record MovieSummary(
    int Id,
    string Title,
    string? Overview,
    string? PosterUrl,
    string? ReleaseDate,
    double Rating,
    int VoteCount,
    string MediaType,
    string? OriginalLanguage
);

public record MovieDetailsResponse(
    int Id,
    string Title,
    string? Overview,
    string? Tagline,
    string? PosterUrl,
    string? BackdropUrl,
    string? ReleaseDate,
    int? RuntimeMinutes,
    double Rating,
    int VoteCount,
    string? Status,
    IEnumerable<string> Genres,
    IEnumerable<string> Countries,
    IEnumerable<string> SpokenLanguages,
    string? OriginalLanguage,
    string? Homepage,
    IEnumerable<TrailerLookupResult> Trailers
);

public record TrailerLookupResult(
    string Key,
    string Name,
    string Site,
    string Type,
    bool Official,
    string WatchUrl
);

public record HealthResponse(string Status, DateTime Utc, string Environment);
