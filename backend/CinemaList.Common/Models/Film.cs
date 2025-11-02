using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;

namespace CinemaList.Common.Models;

public class Film
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    [AllowNull]
    public string Id { get; set; } = null!;
    public required string Title { get; set; } = string.Empty;
    public required string ImdbId { get; set; } = string.Empty;
    
    public required string TmdbId { get; set; } = string.Empty;
    
    public bool IsInRadarr { get; set; } = false;

    public string? Country { get; set; }
    public string? Year { get; set; }
    
    public string? PosterUrl { get; init; }
}