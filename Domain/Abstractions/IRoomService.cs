using Kx.Core.Common.Interfaces;

namespace Domain.Interfaces;

public interface IRoomService
{
    Task CreateRoomsIndexes();
    Task DoRoomsAsync();
    Task<IPaginatedModel<T>> GetDataFromApiAsync<T>(UriBuilder uriBuilder, HttpClient httpClient);
}