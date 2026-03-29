using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MovieTrailer.Models;
using MovieTrailer.Options;

namespace MovieTrailer.Services;

public class TmdbMovieClient(HttpClient http, IDistributedCache cache, IOptions<TmdbOptions> options, ILogger<TmdbMovieClient> logger)
{
    private readonly TmdbOptions _tmdb = options.Value;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<TmdbSearchResponse?> SearchAsync(string query, int page, string language, CancellationToken ct)
    {
        var cacheKey = $"tmdb:search:{language}:{Uri.EscapeDataString(query)}:{page}";
        var cached = await cache.GetStringAsync(cacheKey, token: ct);
        if (cached != null)
            return JsonSerializer.Deserialize<TmdbSearchResponse>(cached, _json);

        var url = $"search/multi?api_key={_tmdb.ApiKey}&query={Uri.EscapeDataString(query)}&page={page}&language={language}&include_adult=false";
        logger.LogDebug("TMDB search: {Query} page={Page} lang={Language}", query, page, language);

        var response = await http.GetAsync(url, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            logger.LogWarning("TMDB rate limit reached");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("TMDB search failed with {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException($"TMDB returned {(int)response.StatusCode}", null, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        await cache.SetStringAsync(cacheKey, json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_tmdb.SearchTtlSeconds) }, ct);

        return JsonSerializer.Deserialize<TmdbSearchResponse>(json, _json);
    }

    public Task<TmdbMovieDetail?> GetMovieAsync(int id, string language, CancellationToken ct) =>
        GetTmdbDetailAsync($"movie/{id}", $"tmdb:movie:{language}:{id}", language, ct);

    public Task<TmdbMovieDetail?> GetSeriesAsync(int id, string language, CancellationToken ct) =>
        GetTmdbDetailAsync($"tv/{id}", $"tmdb:tv:{language}:{id}", language, ct);

    private async Task<TmdbMovieDetail?> GetTmdbDetailAsync(string endpoint, string cacheKey, string language, CancellationToken ct)
    {
        var cached = await cache.GetStringAsync(cacheKey, token: ct);
        if (cached != null)
            return JsonSerializer.Deserialize<TmdbMovieDetail>(cached, _json);

        var url = $"{endpoint}?api_key={_tmdb.ApiKey}&language={language}&append_to_response=videos";
        logger.LogDebug("TMDB detail: {Endpoint} lang={Language}", endpoint, language);

        var response = await http.GetAsync(url, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("TMDB detail failed: {Endpoint} status={StatusCode}", endpoint, (int)response.StatusCode);
            throw new HttpRequestException($"TMDB returned {(int)response.StatusCode}", null, response.StatusCode);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var detailCacheOpts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_tmdb.DetailTtlSeconds) };
        await cache.SetStringAsync(cacheKey, json, detailCacheOpts, ct);

        return JsonSerializer.Deserialize<TmdbMovieDetail>(json, _json);
    }

    public string? PosterUrl(string? path, string size = "w500") =>
        string.IsNullOrEmpty(path) ? null : $"{_tmdb.ImageBaseUrl}/{size}{path}";
}
