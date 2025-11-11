using System.Net.Http;

namespace CinemaList.Api.Clients;

public class OmdbClient(HttpClient client)
{
    public HttpClient Client = client;
}