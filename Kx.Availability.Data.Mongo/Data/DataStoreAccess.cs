﻿using Domain.Abstractions;
using Kx.Core.Common.Data;
using Kx.Core.Common.HelperClasses;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace Persistence.Kx.Availability.Data.Mongo.Data;

public class DataStoreAccess<T> : IDataAggregationStoreAccess<T> where T : class
{
    private readonly IMongoCollection<T> _collection;
    private readonly IMongoDatabase _database;
    private readonly string _collectionName;
    private readonly JsonSerializerSettings _jsonSettings;

    public DataStoreAccess(
        IConnectionDefinitionFactory connectionFactory,
        IKxJsonSettings jsonSettings)
    {
        _jsonSettings = jsonSettings.SerializerSettings;
        _database = connectionFactory.GetMongoDbConnection().GetMongoDatabase();
        _collectionName = typeof(T).Name;
        _collection = _database.GetCollection<T>(_collectionName);
    }

    public async Task InsertPageAsync<TU>(IPaginatedModel<T>? data) where TU : IPaginatedModel<T>
    {
        var payload = JsonConvert.SerializeObject(data, _jsonSettings);
        var models = JsonConvert.DeserializeObject<TU>(payload, _jsonSettings);
        if (models != null)
        {
            await _collection.InsertManyAsync(models.Data);
        }
    }
    public async Task AddIndex(CreateIndexModel<T> indexModel)
    {
        await _collection.Indexes.CreateOneAsync(indexModel);
    }

    public IQueryable<T> QueryFreely() => _collection.AsQueryable();

    public async Task DeleteAsync()
    {
        var collections = await _database.ListCollectionNamesAsync();
        if (collections.ToList().Contains(_collectionName))
        {
            await _database.DropCollectionAsync(_collectionName);
        }
    }
}
