using MongoDB.Driver;

namespace Domain.Abstractions;

public interface IDataAggregationStoreAccess<T> where T:class
{
    Task InsertPageAsync<TU>(IPaginatedModel<T>? data) where TU: IPaginatedModel<T>;
    IQueryable<T>? QueryFreely();
    Task DeleteAsync();
    Task AddIndex(CreateIndexModel<T> indexModel);
}
