using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;
using CinemaList.Scraper.Models;
using HtmlAgilityPack;
using MongoDB.Driver;

namespace CinemaList.Scraper.Scrapers;

/// <summary>
/// Scraper for Cinéma Beaubien (cinemacinema.ca) current films page.
/// Strategy:
/// 1. Fetch listing page "/fr/cinema-beaubien/films".
/// 2. Extract unique anchors whose href contains "/fr/films/".
/// 3. For each film detail page, fetch and parse metadata (Title, Director, Duration, Language, Country, Year if available).
///    The site does not clearly expose Year in the listing; attempt regex fallback.
/// 4. Build <see cref="ScrapedFilm"/> objects; apply existing ShouldBeAdded() predicate.
///    If Year cannot be found, we skip adding unless relaxing predicate in future.
/// </summary>
public class CinemaBeaubienScraper(
    HttpClient httpClient,
    IMongoCollection<ScraperHistoryEntity> collection
) : Scraper(collection, httpClient)
{
    private static class Endpoints
    {
        public const string ListingPath = "/fr/cinema-beaubien/films";
    }

    private static class RegexPatterns
    {
        public static readonly Regex Year = new(
            @"(?<!\d)(19|20)\d{2}(?!\d)",
            RegexOptions.Compiled
        );
        public static readonly Regex DurationMinutes = new(
            @"(?<!\d)(\d{2,3})(?:\s*min|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
    }

    public override async Task<List<ScrapedFilm>> Scrape(
        CancellationToken cancellationToken = default
    )
    {
        string html = await Client.GetStringAsync(Endpoints.ListingPath, cancellationToken);
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // The page appears to be SSR + hydrated (Svelte-style comment markers) and anchor text may be empty.
        // We look for <a> tags with href containing "/fr/films/" and extract the title from the img alt attribute.
        HtmlNodeCollection? anchorNodes = doc.DocumentNode.SelectNodes(
            "//a[contains(@href, '/fr/films/')]"
        );

        var filmEntries = new List<(string Title, string Url)>();
        foreach (var anchor in anchorNodes ?? Enumerable.Empty<HtmlNode>())
        {
            string href = anchor.GetAttributeValue("href", "");
            if (string.IsNullOrWhiteSpace(href) || !href.Contains("/fr/films/"))
                continue;

            // Find img tag within this anchor
            var img = anchor.SelectSingleNode(".//img[@alt]");
            if (img == null)
                continue;

            string rawTitle = HtmlEntity.DeEntitize(img.GetAttributeValue("alt", "").Trim());
            if (string.IsNullOrWhiteSpace(rawTitle))
                continue;

            if (href.StartsWith("/"))
            {
                href = Client.BaseAddress?.ToString().TrimEnd('/') + href;
            }
            filmEntries.Add((rawTitle, href));
        }

        // Deduplicate by URL
        var distinctFilms = filmEntries.GroupBy(x => x.Url).Select(g => g.First()).ToList();

        List<ScrapedFilm> films = new();
        foreach (var entry in distinctFilms)
        {
            ScrapedFilm film = new() { Title = entry.Title, Url = entry.Url };

            await PopulateDetailFieldsAsync(film, cancellationToken);

            if (film.ShouldBeAdded())
            {
                films.Add(film);
            }
        }

        return films;
    }

    private async Task PopulateDetailFieldsAsync(
        ScrapedFilm film,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrEmpty(film.Url))
            return;

        try
        {
            string html = await Client.GetStringAsync(film.Url, cancellationToken);
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Title (override if cleaner on detail page)
            var h1 = doc.DocumentNode.SelectSingleNode("//h1");
            if (h1 != null)
            {
                var detailTitle = HtmlEntity.DeEntitize(h1.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(detailTitle))
                    film.Title = detailTitle;
            }

            // Director(s) - find label 'Réalisation' then following siblings text.
            film.Director ??= ExtractLabeledValue(doc, "Réalisation");
            film.Country ??= ExtractLabeledValue(doc, "Pays");
            film.Language ??= ExtractLabeledValue(doc, "Langue");
            film.Duration ??= ExtractLabeledValue(doc, "Durée");

            // Year: attempt regex search across a credits block or entire document if not explicitly labeled.
            if (string.IsNullOrEmpty(film.Year))
            {
                // Prefer within credits section near Director.
                string creditsText =
                    CollectSectionText(doc, "Crédits") ?? doc.DocumentNode.InnerText;
                var match = RegexPatterns.Year.Match(creditsText);
                if (match.Success)
                {
                    film.Year = match.Value;
                }
            }
        }
        catch (Exception)
        {
            // Swallow for now; could log later via injected logger.
        }
    }

    private static string? ExtractLabeledValue(HtmlDocument doc, string label)
    {
        // Try: exact text node, then following sibling(s). This is heuristic due to unknown markup.
        // 1. Find any element whose normalized text equals the label.
        var labelNode = doc.DocumentNode.SelectSingleNode(
            $"//*[normalize-space(text())='{label}']"
        );
        if (labelNode == null)
            return null;

        // 2. Candidate values: following siblings until next label or empty.
        var siblings = labelNode
            .ParentNode?.ChildNodes.SkipWhile(n => n != labelNode)
            .Skip(1) // skip the label itself
            .TakeWhile(n => !IsLabelNode(n))
            .Where(n => !string.IsNullOrWhiteSpace(n.InnerText.Trim()))
            .ToList();

        if (siblings == null || siblings.Count == 0)
        {
            // Fallback: next element in document order.
            var next = labelNode.SelectSingleNode("following-sibling::*[1]");
            if (next != null && !IsLabelNode(next))
                return HtmlEntity.DeEntitize(next.InnerText.Trim());
            return null;
        }

        string combined = string.Join(
            ", ",
            siblings.Select(s => HtmlEntity.DeEntitize(s.InnerText.Trim()))
        );
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static bool IsLabelNode(HtmlNode node)
    {
        if (node == null)
            return false;
        string text = node.InnerText.Trim();
        return text
            is "Réalisation"
                or "Pays"
                or "Langue"
                or "Durée"
                or "Crédits"
                or "Synopsis"
                or "Genre";
    }

    private static string? CollectSectionText(HtmlDocument doc, string sectionHeader)
    {
        var headerNode = doc.DocumentNode.SelectSingleNode(
            $"//*[normalize-space(text())='{sectionHeader}']"
        );
        if (headerNode == null)
            return null;
        // Collect text from following siblings until next header.
        var texts =
            headerNode
                .ParentNode?.ChildNodes.SkipWhile(n => n != headerNode)
                .Skip(1)
                .TakeWhile(n => !IsLabelNode(n))
                .Select(n => n.InnerText.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t)) ?? Enumerable.Empty<string>();
        var combined = string.Join(" ", texts);
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    public override async Task PersistRunAsync(
        List<Film> films,
        CancellationToken cancellationToken = default
    )
    {
        // Create dictionary, ignoring duplicate titles (keep first occurrence)
        Dictionary<string, string> moviesScraped = new();

        foreach (Film film in films)
        {
            string title = film.Title.ToString();

            // Only add if title not already in dictionary
            if (!moviesScraped.ContainsKey(title))
            {
                moviesScraped[title] = film.TmdbId;
            }
        }

        ScraperHistoryEntity history = new()
        {
            ScrapeDate = DateTime.UtcNow,
            Source = Client.BaseAddress?.ToString() ?? "CinemaBeaubien",
            MoviesScraped = moviesScraped,
        };

        await Collection.InsertOneAsync(history, new InsertOneOptions(), cancellationToken);
    }
}
