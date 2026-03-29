using MovieTrailer.Models;

namespace MovieTrailer.Services;

public class TrailerResolver(YouTubeTrailerClient youtube)
{
    public async Task<IReadOnlyList<TrailerLookupResult>> ResolveAsync(TmdbMovieDetail detail, CancellationToken ct)
    {
        var tmdbTrailers = detail.Videos?.Results
            .Where(v => v.Site == "YouTube")
            .OrderByDescending(v => v.Official).ThenByDescending(v => v.PublishedAt)
            .Select(v => new TrailerLookupResult(v.Key, v.Name, v.Site, v.Type, v.Official,
                $"https://www.youtube.com/watch?v={v.Key}"))
            .ToList() ?? [];

        if (tmdbTrailers.Count > 0)
            return tmdbTrailers;

        var title = detail.Title ?? detail.Name ?? "Unknown";
        var releaseDate = detail.ReleaseDate ?? detail.FirstAirDate;
        var year = releaseDate?.Length >= 4 ? releaseDate[..4] : null;

        return await youtube.FindTrailersAsync(title, year, ct);
    }
}
