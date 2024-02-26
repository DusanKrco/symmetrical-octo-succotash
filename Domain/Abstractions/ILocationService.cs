using Domain.Models;
using Domain.StoredModels;

namespace Domain.Interfaces;

public interface ILocationService
{
    Task CreateLocationsIndexes();
    Task DoLocationsAsync();
    IEnumerable<LocationModel> AddLocationModels(BedroomsDataStoreModel room);
    Task<IPaginatedModel<T>> GetDataFromApiAsync<T>(UriBuilder uriBuilder, HttpClient httpClient);
}