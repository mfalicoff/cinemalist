using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CinemaList.Api.Models;

public record RadarrMovie
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("originalLanguage")]
    public Language? OriginalLanguage { get; init; }
    
    [JsonPropertyName("overview")]
    public string Overview { get; init; } = string.Empty;

    [JsonPropertyName("images")]
    public List<Image> Images { get; init; } = [];

    [JsonPropertyName("remotePoster")]
    public string RemotePoster { get; init; } = string.Empty;

    [JsonPropertyName("youTubeTrailerId")]
    public string YouTubeTrailerId { get; init; } = string.Empty;

    [JsonPropertyName("runtime")]
    public int Runtime { get; init; }

    [JsonPropertyName("imdbId")]
    public string ImdbId { get; init; } = string.Empty;

    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; init; }

    [JsonPropertyName("ratings")]
    public Ratings? Ratings { get; init; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; init; }
}

public record Language
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public record Image
{
    [JsonPropertyName("coverType")]
    public string CoverType { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("remoteUrl")]
    public string RemoteUrl { get; init; } = string.Empty;
}

public record Ratings
{
    [JsonPropertyName("imdb")]
    public Rating? Imdb { get; init; }

    [JsonPropertyName("tmdb")]
    public Rating? Tmdb { get; init; }

    [JsonPropertyName("metacritic")]
    public Rating? Metacritic { get; init; }

    [JsonPropertyName("rottenTomatoes")]
    public Rating? RottenTomatoes { get; init; }

    [JsonPropertyName("trakt")]
    public Rating? Trakt { get; init; }
}

public record Rating
{
    [JsonPropertyName("votes")] public int Votes { get; init; }

    [JsonPropertyName("value")] public double Value { get; init; }

    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
}