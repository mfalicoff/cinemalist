using System.Collections.Generic;
using System.Threading;
using CinemaList.Api.Endpoints;
using Scalar.AspNetCore;
using CinemaList.Api.Extensions;
using CinemaList.Api.Services;
using CinemaList.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CinemaList.Scraper.Models;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;



WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.ConfigureConfiguration();

builder.Services.AddOpenApi();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddMovieServices(builder.Configuration);
builder.Services.AddScraping(builder.Configuration);

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

app.Run();
