# Movie Trailer API

ASP.NET Core 8 Web API for searching movies and TV series with trailer support. Integrates TMDB as the primary data source and falls back to YouTube Data API v3 when TMDB has no trailer data for a title.

## Stack

- .NET 8 / ASP.NET Core Web API
- TMDB API v3
- YouTube Data API v3
- Redis (optional — falls back to in-memory cache)
- Serilog structured logging
- AspNetCoreRateLimit

## Getting Started

You need API keys for TMDB and YouTube. Store them in user-secrets — never in `appsettings.json`.

```bash
cd MovieTrailer
dotnet user-secrets set "Tmdb:ApiKey" "your-tmdb-key"
dotnet user-secrets set "YouTube:ApiKey" "your-youtube-key"
dotnet run
```

Swagger UI available at `https://localhost:{port}/swagger` in Development.

### TMDB API Key

1. Register at https://www.themoviedb.org/signup
2. Go to Settings → API → Create (Developer)
3. Copy the **API Key (v3 auth)**

### YouTube API Key

1. Go to https://console.cloud.google.com
2. Enable **YouTube Data API v3**
3. Create Credentials → API key

## Endpoints

### Search

```
GET /api/movies/search?q={query}&page={n}&language={locale}
```

| Param | Default | Notes |
|---|---|---|
| `q` | required | max 200 chars |
| `page` | 1 | 1–500 |
| `language` | en-US | see supported locales below |

Returns movies and TV series matching the query. Persons are excluded from results.

**Supported locales:** `en-US` `de-DE` `fr-FR` `nl-NL` `es-ES` `it-IT` `pt-BR` `ja-JP` `ko-KR` `zh-CN`

### Detail

```
GET /api/movies/{id}?type={movie|tv}&language={locale}
```

Returns full details for a title — genres, runtime, countries, rating, status, and a list of trailers with YouTube watch URLs.

### Health

```
GET /health
```

Returns API status, UTC timestamp, and active environment.

## Configuration

| Key | Default | Description |
|---|---|---|
| `Tmdb:ApiKey` | — | Required. TMDB v3 API key |
| `YouTube:ApiKey` | — | Required. YouTube Data API v3 key |
| `Tmdb:TimeoutSeconds` | 6 | HTTP timeout for TMDB calls |
| `YouTube:TimeoutSeconds` | 5 | HTTP timeout for YouTube calls |
| `Redis:ConnectionString` | *(empty)* | Leave empty to use in-memory cache |
| `Tmdb:SearchTtlSeconds` | 300 | Cache TTL for search results |
| `Tmdb:DetailTtlSeconds` | 3600 | Cache TTL for detail pages |
| `AllowedOrigins` | *(empty)* | Comma-separated CORS origins for Frontend policy |

For production, pass keys as environment variables:

```bash
Tmdb__ApiKey=your-key
YouTube__ApiKey=your-key
```

## Caching

All TMDB and YouTube calls are cache-first.

| Data | TTL | Reason |
|---|---|---|
| Search results | 5 min | Queries vary, data changes |
| Movie / TV details | 1 hour | Stable, rarely changes |
| YouTube trailers | 6 hours | Conserves API quota |

Redis is used when `Redis:ConnectionString` is set. Falls back to `IDistributedMemoryCache` with no code changes.

## Rate Limiting

IP-based via AspNetCoreRateLimit:

- Global: 100 req/min
- `/api/movies/search`: 30 req/min

## Project Structure

```
MovieTrailer/
├── Controllers/        # MoviesController
├── Services/           # MovieDiscoveryService, TmdbMovieClient, YouTubeTrailerClient
├── Models/             # TMDB response shapes, API DTOs
├── Options/            # TmdbOptions, YouTubeOptions — strongly typed config with ValidateOnStart
├── Exceptions/         # MovieNotFoundException
├── Middleware/         # Global exception handling
└── Extensions/         # DI registration — AddApplicationServices, AddCaching, AddExternalClients, AddApiCors, AddRateLimiting
```

## Architecture Notes

The API is intentionally flat — one project, no Clean Architecture layers. Two read-only endpoints that proxy and aggregate external data don't justify MediatR, CQRS, or a repository pattern.

`TmdbOptions` and `YouTubeOptions` use `[Required]` with `ValidateOnStart()` — the app refuses to start if API keys are missing rather than failing on the first request.

Trailers are fetched via `append_to_response=videos` on the TMDB detail call — one HTTP request returns both movie details and trailers. YouTube is only queried when TMDB returns no video data.
