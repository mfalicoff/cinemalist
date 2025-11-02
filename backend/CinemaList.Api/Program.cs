using CinemaList.Api.Endpoints;
using Scalar.AspNetCore;
using CinemaList.Api.Extensions;
using CinemaList.Api.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.ConfigureConfiguration();

builder.Services
    .BindFromConfiguration<OMDbSettings>(builder.Configuration)
    .BindFromConfiguration<RadarrSettings>(builder.Configuration)
    .BindFromConfiguration<MongoDbSettings>(builder.Configuration);

builder.Services.AddOpenApi();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddMovieServices();
builder.Services.AddScraping();

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

FilmEndpoints.MapFilmEndpoints(app);
ScraperEndpoints.MapScraperEndpoints(app);
app.MapHealthChecks("/health");

app.Run();
