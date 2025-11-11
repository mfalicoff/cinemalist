using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaList.Common.Models;

public class OmdbResponse
{
    public List<OmdbMovie> Search { get; set; } = [];
    public string TotalResults { get; set; }
    public string Response { get; set; }

    public static async Task<OmdbResponse?> CreateFromResponse(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default
    )
    {
        string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OmdbResponse>(jsonResponse);
    }
}

public record OmdbMovie
{
    public required string Title { get; set; }

    [JsonPropertyName("imdbID")]
    public required string ImdbId { get; set; }
}
