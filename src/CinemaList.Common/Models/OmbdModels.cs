using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaList.Common.Models;

public class OmdbResponse
{
    public List<OmdbMovie> Search { get; set; }
    public string TotalResults { get; set; }
    public string Response { get; set; }

    public static async Task<OmdbResponse?> CreateFromResponse(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OmdbResponse>(jsonResponse);
    }
}

public class OmdbMovie
{
    public string Title { get; set; }
    public string Year { get; set; }
    public string imdbID { get; set; }
    public string Type { get; set; }
    public string Poster { get; set; }
}