using System.ComponentModel.DataAnnotations;

namespace MovieTrailer.Options;

public class YouTubeOptions
{
    public const string Section = "YouTube";

    [Required]
    public string ApiKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://www.googleapis.com/youtube/v3";
    public int TimeoutSeconds { get; init; } = 5;
}
