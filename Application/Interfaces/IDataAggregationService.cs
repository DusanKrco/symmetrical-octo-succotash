using Domain.Abstractions;
using System.Net;

namespace Application.Interfaces;

public interface IDataAggregationService
{
    Task<(HttpStatusCode statusCode, string result)> ReloadOneTenantsDataAsync();
    Task InsertStateAsync(ITenantDataModel item);
}
