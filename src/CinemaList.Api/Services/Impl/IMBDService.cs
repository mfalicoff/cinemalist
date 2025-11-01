using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CinemaList.Api.Settings;
using CinemaList.Common.Models;
using Microsoft.Extensions.Options;

namespace CinemaList.Api.Services.Impl;

public class IMBDService(HttpClient httpClient, IOptions<OMDbSettings> settings): IIMBDService
{
    private readonly HttpClient _httpClient = httpClient;

    private readonly OMDbSettings _omDbSettings = settings.Value;
    
    public async Task<OmdbMovie?> GetSearchMovie(ScrapedFilm scrapedFilm, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"?apikey={_omDbSettings.ApiKey}&s={HttpUtility.HtmlEncode(scrapedFilm.Title)}&y={scrapedFilm.Year}", cancellationToken);        
        if (response.IsSuccessStatusCode)
        {
            OmdbResponse? omdbResponse = await OmdbResponse.CreateFromResponse(response, cancellationToken);
            
            if (omdbResponse is { Response: "True", Search.Count: > 0 })
            {
                return omdbResponse.Search.FirstOrDefault(x => x.Title.Equals(scrapedFilm.Title));
            }
        }
        return null;
    }
}