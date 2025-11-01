using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CinemaList.Common.Models;
using MongoDB.Driver;

namespace CinemaList.Api.Repository.Impl;

public class FilmRepository(IMongoCollection<Film> collection): IFIlmRepository
{
    private readonly IMongoCollection<Film> _collection = collection;

    public Task<List<Film>> GetAllFilms()
    {
        return _collection.Find(_ => true).ToListAsync();
    }
    
    public async Task UpsertFilms(List<Film> films, CancellationToken cancellationToken = default)
    {
        List<WriteModel<Film>> bulkOps = (from film in films
                let filter = Builders<Film>.Filter.Eq(f => f.IMBDId, film.IMBDId)
                let update = Builders<Film>.Update.Set(f => f.Title, film.Title)
                    .Set(f => f.Country, film.Country)
                    .Set(f => f.Year, film.Year)
                    .Set(f => f.Director, film.Director)
                    .Set(f => f.PosterUrl, film.PosterUrl)
                select new UpdateOneModel<Film>(filter, update) { IsUpsert = true }).Cast<WriteModel<Film>>()
            .ToList();
        
        await _collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken);
    }
}