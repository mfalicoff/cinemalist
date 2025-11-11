using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson.Serialization.Attributes;

namespace CinemaList.Scraper.Models;

public class ScraperHistoryEntity
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    [AllowNull]
    public string Id { get; set; }
    
    public required DateTime ScrapeDate { get; set; }
    
    public required string Source { get; set; }
    
    public required Dictionary<string, string> MoviesScraped { get; set; }
}