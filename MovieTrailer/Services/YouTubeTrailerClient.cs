using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MovieTrailer.Models;
using MovieTrailer.Options;

namespace MovieTrailer.Services;

public class YouTubeTrailerClient(HttpClient http, IDistributedCache cache, IOptions<YouTubeOptions> options, ILogger<YouTubeTrailerClient> logger)
{
    private readonly YouTubeOptions _yt = options.Value;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<TrailerLookupResult>> FindTrailersAsync(string title, string? year, CancellationToken ct)
    {
        var query = year != null ? $"{title} {year} official trailer" : $"{title} official trailer";
        var cacheKey = $"yt:trailers:{Uri.EscapeDataString(query)}";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
        {
            var cachedItems = JsonSerializer.Deserialize<List<YouTubeSearchItem>>(cached, _json) ?? [];
            return cachedItems.Where(i => i.Id?.VideoId != null).Select(ToTrailerResult).ToList();
        }

        logger.LogDebug("YouTube trailer search: {Title} ({Year})", title, year ?? "n/a");

        var url = $"search?part=snippet&q={Uri.EscapeDataString(query)}&type=video&maxResults=5&key={_yt.ApiKey}";
        var response = await http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("YouTube search failed with {StatusCode} for '{Title}'", response.StatusCode, title);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var items = JsonSerializer.Deserialize<YouTubeSearchResponse>(json, _json)?.Items ?? [];

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(items),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) }, ct);

        return items.Where(i => i.Id?.VideoId != null).Select(ToTrailerResult).ToList();
    }

    private static TrailerLookupResult ToTrailerResult(YouTubeSearchItem i) => new(
        Key: i.Id!.VideoId!,
        Name: i.Snippet?.Title ?? "Trailer",
        Site: "YouTube",
        Type: "Trailer",
        Official: i.Snippet?.ChannelTitle?.Contains("official", StringComparison.OrdinalIgnoreCase) ?? false,
        WatchUrl: $"https://www.youtube.com/watch?v={i.Id!.VideoId}"
    );
}
