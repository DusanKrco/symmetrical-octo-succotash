namespace Domain.Abstractions;

public interface IRoomService
{
    Task CreateRoomsIndexes();
    Task DoRoomsAsync();
    Task<IPaginatedModel<T>> GetDataFromApiAsync<T>(UriBuilder uriBuilder, HttpClient httpClient);
}