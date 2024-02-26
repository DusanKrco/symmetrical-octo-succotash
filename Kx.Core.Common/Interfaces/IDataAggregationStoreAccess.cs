using MongoDB.Driver;

namespace Domain.Interfaces;

public interface IDataAggregationStoreAccess<T> where T:class
{
    Task InsertPageAsync<TU>(IPaginatedModel<T>? data) where TU: IPaginatedModel<T>;
    IQueryable<T>? QueryFreely();
    Task DeleteAsync();
    Task AddIndex(CreateIndexModel<T> indexModel);
}
