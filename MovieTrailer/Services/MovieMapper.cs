using MovieTrailer.Models;

namespace MovieTrailer.Services;

internal static class MovieMapper
{
    internal static MovieSummary ToSummary(TmdbSearchResult r, string? posterUrl) => new(
        Id: r.Id,
        Title: r.Title ?? r.Name ?? "Unknown",
        Overview: r.Overview,
        PosterUrl: posterUrl,
        ReleaseDate: r.ReleaseDate ?? r.FirstAirDate,
        Rating: Math.Round(r.VoteAverage, 1),
        VoteCount: r.VoteCount,
        MediaType: r.MediaType ?? "movie",
        OriginalLanguage: r.OriginalLanguage
    );
}
