using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;
using CinemaList.Scraper.Models;
using HtmlAgilityPack;
using MongoDB.Driver;

namespace CinemaList.Scraper.Scrapers;

public class CinemaModerneScraper(HttpClient httpClient, IMongoCollection<ScraperHistoryEntity> collection): Scraper
{
    private readonly HttpClient _httpClient = httpClient;
    
    private readonly IMongoCollection<ScraperHistoryEntity> _collection = collection;

    /// <summary>
    /// Contains all HTML selectors and CSS classes used for scraping the Cinema Moderne website.
    /// </summary>
    private static class HtmlSelectors
    {
        public const string FilmCardContainer = "//div[contains(@class, 'cm-Card')]";
        public const string TitleLink = ".//a[contains(@class, 'cm-Card__title')]";
        public const string FilmLink = ".//a[@href]";
        public const string Directors = ".//p[@class='cm-Card__overlay-directors']";
        public const string DirectorSpans = ".//span";
        public const string Countries = ".//p[@class='cm-Card__overlay-countries']";
        public const string CountrySpans = ".//span";
        public const string Year = ".//p[@class='cm-Card__overlay-year']/span";
        public const string Duration = ".//p[@class='cm-Card__overlay-length']/span";
        public const string Language = ".//p[@class='cm-Card__overlay-lang']/span";
    }

    public override async Task<List<ScrapedFilm>> Scrape(CancellationToken cancellationToken = default)
    {
        string html = await _httpClient.GetStringAsync("#cinema-en-salle", cancellationToken);
        
        List<ScrapedFilm> films = [];
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // Find all film cards
        HtmlNodeCollection? filmCards = doc.DocumentNode.SelectNodes(HtmlSelectors.FilmCardContainer);
        
        if (filmCards == null) return films;

        foreach (HtmlNode card in filmCards)
        {
            ScrapedFilm film = ExtractFilmFromCard(card);
            
            if (film.ShouldBeAdded() )
            {
                films.Add(film);
            }
        }
        
        return films;
    }

    public override async Task<bool> ShouldRunScraper(CancellationToken cancellationToken = default)
    {
        DateTime oneWeekAgo = DateTime.UtcNow.AddDays(-1);
        FilterDefinition<ScraperHistoryEntity> filter =
            Builders<ScraperHistoryEntity>.Filter.Gt(x => x.ScrapeDate, oneWeekAgo) &
            Builders<ScraperHistoryEntity>.Filter.Eq(x => x.Source, _httpClient.BaseAddress?.ToString());
        
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) == 0;

    }

    public override async Task PersistRunAsync(List<Film> films, CancellationToken cancellationToken = default)
    {
        ScraperHistoryEntity history = new()
        {
            ScrapeDate = DateTime.UtcNow,
            Source = _httpClient.BaseAddress?.ToString() ?? "CinemaModerne",
            MoviesScraped = new Dictionary<string, string>(films.Select(x => new KeyValuePair<string, string>(x.Title.ToString(), x.ImdbId)))
        };

        await _collection.InsertOneAsync(history, new InsertOneOptions(), cancellationToken);
    }
    
    private ScrapedFilm ExtractFilmFromCard(HtmlNode card)
    {
        ScrapedFilm film = new();

        // Extract title
        HtmlNode? titleNode = card.SelectSingleNode(HtmlSelectors.TitleLink);
        if (titleNode != null)
        {
            film.Title = System.Net.WebUtility.HtmlDecode(titleNode.InnerText.Trim());
        }
        
        // Extract URL
        HtmlNode? linkNode = card.SelectSingleNode(HtmlSelectors.FilmLink);
        if (linkNode != null)
        {
            film.Url = linkNode.GetAttributeValue("href", "");
        }
        
        // Extract director(s)
        HtmlNode? directorNode = card.SelectSingleNode(HtmlSelectors.Directors);
        if (directorNode != null)
        {
            HtmlNodeCollection? directors = directorNode.SelectNodes(HtmlSelectors.DirectorSpans);
            if (directors != null)
            {
                film.Director = string.Join(", ", directors.Select(d => 
                    System.Net.WebUtility.HtmlDecode(d.InnerText.Trim())));
            }
        }
        
        // Extract country
        HtmlNode? countryNode = card.SelectSingleNode(HtmlSelectors.Countries);
        if (countryNode != null)
        {
            HtmlNodeCollection? countries = countryNode.SelectNodes(HtmlSelectors.CountrySpans);
            if (countries != null)
            {
                film.Country = string.Join(", ", countries.Select(c => 
                    System.Net.WebUtility.HtmlDecode(c.InnerText.Trim())));
            }
        }
        
        // Extract year
        HtmlNode? yearNode = card.SelectSingleNode(HtmlSelectors.Year);
        if (yearNode != null)
        {
            film.Year = System.Net.WebUtility.HtmlDecode(yearNode.InnerText.Trim());
        }
        
        // Extract duration
        HtmlNode? durationNode = card.SelectSingleNode(HtmlSelectors.Duration);
        if (durationNode != null)
        {
            film.Duration = System.Net.WebUtility.HtmlDecode(durationNode.InnerText.Trim());
        }
        
        // Extract language
        HtmlNode? langNode = card.SelectSingleNode(HtmlSelectors.Language);
        if (langNode != null)
        {
            film.Language = System.Net.WebUtility.HtmlDecode(langNode.InnerText.Trim());
        }

        return film;
    }
}