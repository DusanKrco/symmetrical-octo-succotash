namespace Domain.Abstractions;

public interface ITestData
{
    Task InsertAsync(IDataModel item);
    Task<object?> GetAllItemsAsync(string tableName);
    Task DeleteTableAsync();
    Task DeleteStateTableAsync();
}
