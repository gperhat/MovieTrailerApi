using MovieTrailer.Exceptions;
using MovieTrailer.Models;

namespace MovieTrailer.Services;

public class MovieDiscoveryService(TmdbMovieClient tmdb, TrailerResolver trailerResolver)
{
    public async Task<MovieSearchResultsPage> SearchAsync(string query, int page, string language, CancellationToken ct)
    {
        var result = await tmdb.SearchAsync(query, page, language, ct);

        var items = result.Results
           .Where(r => Enum.TryParse<MediaType>(r.MediaType, ignoreCase: true, out _))
           .Select(r => MovieMapper.ToSummary(r, tmdb.PosterUrl(r.PosterPath)));

        return new MovieSearchResultsPage(result.Page, result.TotalPages, result.TotalResults, items);
    }

    public async Task<MovieDetailsResponse> GetDetailsAsync(int id, MediaType mediaType, string language, CancellationToken ct)
    {
        var detail = mediaType == MediaType.Tv
            ? await tmdb.GetSeriesAsync(id, language, ct)
            : await tmdb.GetMovieAsync(id, language, ct);

        if (detail == null)
            throw new MovieNotFoundException(id, mediaType);

        var releaseDate = detail.ReleaseDate ?? detail.FirstAirDate;
        var trailers = await trailerResolver.ResolveAsync(detail, ct);

        return new MovieDetailsResponse(
            Id: detail.Id,
            Title: detail.Title ?? detail.Name ?? "Unknown",
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
