using CinemaList.Api.Endpoints;
using CinemaList.Api.Extensions;
using CinemaList.Api.Settings;
using Mazilious.Common.Configuration;
using Mazilious.Common.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.ConfigureConfiguration();

builder
    .Services.BindFromConfiguration<TmdbSettings>(builder.Configuration)
    .BindFromConfiguration<RadarrSettings>(builder.Configuration)
    .BindFromConfiguration<MongoDbSettings>(builder.Configuration);

builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddMovieServices(builder.Configuration);
builder.Services.AddScraping(builder.Configuration);

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
