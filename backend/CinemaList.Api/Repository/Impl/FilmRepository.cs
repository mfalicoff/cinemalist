using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;
using MongoDB.Driver;

namespace CinemaList.Api.Repository.Impl;

public class FilmRepository(IMongoCollection<Film> collection): IFilmRepository
{
    private readonly IMongoCollection<Film> _collection = collection;

    public Task<List<Film>> GetAllFilms()
    {
        return _collection.Find(_ => true).ToListAsync();
    }

    public async Task<List<Film>> GetFilmsByFilter(FilmFilter filter, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Film> mongoFilter = filter switch
        {
            FilmFilter.All => Builders<Film>.Filter.Empty,
            FilmFilter.InRadarr => Builders<Film>.Filter.Eq(f => f.IsInRadarr, true),
            FilmFilter.NotInRadarr => Builders<Film>.Filter.Eq(f => f.IsInRadarr, false),
            _ => Builders<Film>.Filter.Empty
        };
        return await (await _collection.FindAsync(mongoFilter, cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
    }

    public async Task UpsertFilms(List<Film> films, CancellationToken cancellationToken = default)
    {
        List<WriteModel<Film>> bulkOps = (from film in films
                let filter = Builders<Film>.Filter.Or(
                    Builders<Film>.Filter.Eq(f => f.ImdbId, film.ImdbId),
                    Builders<Film>.Filter.Eq(f => f.TmdbId, film.TmdbId))
                let update = Builders<Film>.Update
                    .Set(f => f.Title, film.Title)
                    .Set(f => f.Country, film.Country)
                    .Set(f => f.Year, film.Year)
                    .Set(f => f.PosterUrl, film.PosterUrl)
                    .Set(f => f.TmdbId, film.TmdbId)
                    .Set(f => f.IsInRadarr, film.IsInRadarr)
                select new UpdateOneModel<Film>(filter, update) { IsUpsert = true }).Cast<WriteModel<Film>>()
            .ToList();
        
        await _collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken);
    }

    public Task UpdateFilmRadarrStatus(string tmdbId, bool isInRadarr, CancellationToken cancellationToken = default)
    {
        FilterDefinition<Film>? filter = Builders<Film>.Filter.Eq(f => f.TmdbId, tmdbId);
        UpdateDefinition<Film>? update = Builders<Film>.Update.Set(f => f.IsInRadarr, isInRadarr);
        return _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task<Film> GetFilmById(string id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(f => f.ImdbId == id).FirstOrDefaultAsync(cancellationToken);
    }
}