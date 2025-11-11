using System.Net.Http;

namespace CinemaList.Api.Clients;

public class RadarrClient(HttpClient client)
{
    public HttpClient Client = client;
}
