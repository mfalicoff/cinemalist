using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;

namespace CinemaList.Common.Models;

public class Film
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    [AllowNull]
    public string Id { get; set; }
    public required string Title { get; set; }
    public required string IMBDId { get; set; }

    public string? Director { get; set; }
    public string? Country { get; set; }
    public string? Year { get; set; }
    
    public string? PosterUrl { get; set; }
    
    public static Film FromScrapedFilmAndIMDBId(ScrapedFilm scrapedFilm, OmdbMovie omdbMovie)
    {
        return new Film
        {
            Title = scrapedFilm.Title!,
            IMBDId = omdbMovie.imdbID,
            Director = scrapedFilm.Director,
            Country = scrapedFilm.Country,
            Year = scrapedFilm.Year,
            PosterUrl = omdbMovie.Poster
        };
    }
}