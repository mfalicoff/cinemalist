namespace CinemaList.Common.Models;

public record ScrapedFilm
{
    public string? Title { get; set; }
    public string? Director { get; set; }
    public string? Country { get; set; }
    public string? Year { get; set; }
    public string? Duration { get; set; }
    public string? Language { get; set; }
    public string? Url { get; set; }

    public bool ShouldBeAdded()
    {
        return !string.IsNullOrEmpty(Title);
    }
}