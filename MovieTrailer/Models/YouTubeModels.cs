using System.Text.Json.Serialization;

namespace MovieTrailer.Models;

public class YouTubeSearchResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeSearchItem> Items { get; set; } = [];
}

public class YouTubeSearchItem
{
    [JsonPropertyName("id")]
    public YouTubeVideoId? Id { get; set; }

    [JsonPropertyName("snippet")]
    public YouTubeSnippet? Snippet { get; set; }
}

public class YouTubeVideoId
{
    [JsonPropertyName("videoId")]
    public string? VideoId { get; set; }
}

public class YouTubeSnippet
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("channelTitle")]
    public string? ChannelTitle { get; set; }

    [JsonPropertyName("publishedAt")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("thumbnails")]
    public YouTubeThumbnails? Thumbnails { get; set; }
}

public class YouTubeThumbnails
{
    [JsonPropertyName("medium")]
    public YouTubeThumbnail? Medium { get; set; }

    [JsonPropertyName("high")]
    public YouTubeThumbnail? High { get; set; }
}

public class YouTubeThumbnail
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
