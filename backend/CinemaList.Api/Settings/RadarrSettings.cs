namespace CinemaList.Api.Settings;

public record RadarrSettings
{
    public required string BaseUrl { get; init; }

    public required string ApiKey { get; init; }

    public required int QualityProfileId { get; init; } = 5;

    public required string RootFolderPath { get; init; } = "/mnt/media/movies";
}
