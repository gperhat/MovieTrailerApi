using MovieTrailer.Exceptions;
using MovieTrailer.Models;

namespace MovieTrailer.Services;

public class MovieDiscoveryService(TmdbMovieClient tmdb, YouTubeTrailerClient youtube)
{
    private static readonly string[] _tmdbMediaTypes = ["movie", "tv"];

    public async Task<MovieSearchResultsPage?> SearchAsync(string query, int page, string language, CancellationToken ct)
    {
        var result = await tmdb.SearchAsync(query, page, language, ct);
        if (result == null)
            return null;

        var items = result.Results
            .Where(r => _tmdbMediaTypes.Contains(r.MediaType))
            .Select(r => new MovieSummary(
                Id: r.Id,
                Title: r.Title ?? r.Name ?? "Unknown",
                Overview: r.Overview,
                PosterUrl: tmdb.PosterUrl(r.PosterPath),
                ReleaseDate: r.ReleaseDate ?? r.FirstAirDate,
                Rating: Math.Round(r.VoteAverage, 1),
                VoteCount: r.VoteCount,
                MediaType: r.MediaType ?? "movie",
                OriginalLanguage: r.OriginalLanguage
            ));

        return new MovieSearchResultsPage(result.Page, result.TotalPages, result.TotalResults, items);
    }

    public async Task<MovieDetailsResponse> GetDetailsAsync(int id, string mediaType, string language, CancellationToken ct)
    {
        var detail = mediaType == "tv"
            ? await tmdb.GetSeriesAsync(id, language, ct)
            : await tmdb.GetMovieAsync(id, language, ct);

        if (detail == null)
            throw new MovieNotFoundException(id, mediaType);

        var title = detail.Title ?? detail.Name ?? "Unknown";
        var releaseDate = detail.ReleaseDate ?? detail.FirstAirDate;
        var releaseYear = releaseDate?.Length >= 4 ? releaseDate[..4] : null;

        var trailers = detail.Videos?.Results
            .Where(v => v.Site == "YouTube")
            .OrderByDescending(v => v.Official).ThenByDescending(v => v.PublishedAt)
            .Select(v => new TrailerLookupResult(v.Key, v.Name, v.Site, v.Type, v.Official,
                $"https://www.youtube.com/watch?v={v.Key}"))
            .ToList() ?? [];

        if (!trailers.Any())
            trailers = (await youtube.FindTrailersAsync(title, releaseYear, ct)).ToList();

        return new MovieDetailsResponse(
            Id: detail.Id,
            Title: title,
            Overview: detail.Overview,
            Tagline: detail.Tagline,
            PosterUrl: tmdb.PosterUrl(detail.PosterPath, "w500"),
            BackdropUrl: tmdb.PosterUrl(detail.BackdropPath, "w1280"),
            ReleaseDate: releaseDate,
            RuntimeMinutes: detail.Runtime ?? detail.EpisodeRunTime?.FirstOrDefault(),
            Rating: Math.Round(detail.VoteAverage, 1),
            VoteCount: detail.VoteCount,
            Status: detail.Status,
            Genres: detail.Genres.Select(g => g.Name),
            Countries: detail.ProductionCountries.Select(c => c.Name),
            SpokenLanguages: detail.SpokenLanguages.Select(l => l.Name),
            OriginalLanguage: detail.OriginalLanguage,
            Homepage: detail.Homepage,
            Trailers: trailers
        );
    }
}
