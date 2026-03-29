using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MovieTrailer.Exceptions;
using MovieTrailer.Models;
using MovieTrailer.Options;
using MovieTrailer.Services;

namespace MovieTrailer.Controllers;

[ApiController]
[Route("api/movies")]
[Produces("application/json")]
public class MoviesController(MovieDiscoveryService discovery, IOptions<TmdbOptions> tmdbOptions) : ControllerBase
{
    private readonly HashSet<string> _supportedLanguages =
        tmdbOptions.Value.SupportedLanguages.ToHashSet();

    [HttpGet("search")]
    [ProducesResponseType(typeof(MovieSearchResultsPage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] string language = "en-US",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length > 200)
            return BadRequest(new { message = "Query must be between 1 and 200 characters." });

        if (page < 1 || page > 500)
            return BadRequest(new { message = "Page must be between 1 and 500." });

        var lang = _supportedLanguages.Contains(language) ? language : "en-US";
        var results = await discovery.SearchAsync(q.Trim(), page, lang, ct);

        if (results == null)
            return StatusCode(429, new { message = "Upstream service is temporarily unavailable. Try again shortly." });

        return Ok(results);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MovieDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Detail(
        int id,
        [FromQuery] string type = "movie",
        [FromQuery] string language = "en-US",
        CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest(new { message = "Invalid id." });

        var mediaType = type == "tv" ? "tv" : "movie";
        var lang = _supportedLanguages.Contains(language) ? language : "en-US";

        try
        {
            var detail = await discovery.GetDetailsAsync(id, mediaType, lang, ct);
            return Ok(detail);
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
