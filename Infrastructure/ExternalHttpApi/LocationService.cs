using Application.Interfaces;
using Domain.Interfaces;
using Kx.Core.Common.Data;
using Kx.Core.Common.Exceptions;
using Kx.Core.Common.HelperClasses;
using Kx.Core.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Persistence.Kx.Availability.Data.Mongo;
using Persistence.Kx.Availability.Data.Mongo.Models;
using Persistence.Kx.Availability.Data.Mongo.StoredModels;
using Serilog;
using System.Net.Http.Json;

namespace Kx.Availability.Data.Implementation;

public class LocationService : ILocationService
{
    private readonly IDataAggregationStoreAccess<LocationsDataStoreModel> _locationsData;
    private readonly IDataAccessAggregation _aggregateData;
    private readonly string? _coreLocationsUrl;
    private readonly ITenant _tenant;
    private readonly int _pageSize;
    private readonly IHttpClientFactory _httpClientFactory;

    public LocationService(IDataAccessFactory dataAccessFactory, IConfiguration config,
        ITenant tenant, IHttpClientFactory httpClientFactory)
    {
        _tenant = tenant;
        _httpClientFactory = httpClientFactory;

        var dbAccessAggregate = dataAccessFactory.GetDataAccess(KxDataType.AvailabilityAggregation);
        _aggregateData = DataAccessHelper.ParseAggregationDataAccess(dbAccessAggregate);

        _locationsData = dataAccessFactory.GetDataStoreAccess<LocationsDataStoreModel>();
        _coreLocationsUrl = config.GetSection("LOCATIONS_URL").Value;

        _pageSize = 1000;
        if (int.TryParse(config.GetSection("DEFAULT_PAGE_SIZE").Value, out var pageSize))
        {
            _pageSize = pageSize;
        }
    }

    public async Task CreateLocationsIndexes()
    {
        var indexBuilder = Builders<LocationsDataStoreModel>.IndexKeys;
        var indexModel = new CreateIndexModel<LocationsDataStoreModel>(indexBuilder
            .Ascending(x => x.ExternalId)
            .Ascending(x => x.Type)
            .Ascending(x => x.Id)
            .Ascending(p => p.ParentId));
        await _locationsData.AddIndex(indexModel);
    }

    public IEnumerable<LocationModel> AddLocationModels(BedroomsDataStoreModel room)
    {
        try
        {
            var locationsQuery = _locationsData.QueryFreely();

            /* Add the direct parent area */
            var tempLocations =
                locationsQuery?
                    .Where(l => l.Id == room.LocationID
                                && (l.Type.ToLower() != "area" && l.Type.ToLower() != "site"))
                    .Select(loc => new LocationModel
                    {
                        Id = loc.Id,
                        Name = loc.Name,
                        ParentId = loc.ParentId,
                        IsDirectLocation = true
                    }).ToList();


            if (!(tempLocations?.Count > 0))
                return tempLocations as IEnumerable<LocationModel> ?? new List<LocationModel>();


            var currentTopLevelAreaIndex = 0;

            while (!tempLocations.Exists(x => x.ParentId == null))
            {
                var parentLocation = tempLocations[currentTopLevelAreaIndex].ParentId;

                var nextParentLocation =
                    locationsQuery?
                        .Where(l => l.Id == parentLocation)
                        .Select(loc => new LocationModel
                        {
                            Id = loc.Id,
                            Name = loc.Name,
                            ParentId = loc.ParentId,
                            IsDirectLocation = true
                        });

                if (nextParentLocation != null && nextParentLocation.Any())
                {
                    tempLocations.AddRange(nextParentLocation.ToList());
                    currentTopLevelAreaIndex++;
                }
                else
                {
                    Log.Error(
                        $"The location has a parent Id where the location does not exist ParentId: {parentLocation}");
                    break;
                }

                if (currentTopLevelAreaIndex >= tempLocations.Count) break;

            }


            return tempLocations as IEnumerable<LocationModel> ?? new List<LocationModel>();

        }
        catch (Exception ex)
        {
            Task.FromResult(async () => await LogStateErrorsAsync(LocationType.Locations, ex));
            throw;
        }
    }

    public async Task DoLocationsAsync()
    {
        try
        {
            //paginate
            var pageOfLocations = await GetLocationsAsync(pageNo: 1);
            await _locationsData.InsertPageAsync<PaginatedStoreModel<LocationsDataStoreModel>>(pageOfLocations);

            if (pageOfLocations.TotalPages > 1)
            {
                for (var i = 2; i <= pageOfLocations.TotalPages; i++)
                {
                    var page = await GetLocationsAsync(pageNo: i);
                    await _locationsData.InsertPageAsync<PaginatedStoreModel<LocationsDataStoreModel>>(page);
                }
            }
        }
        catch (Exception ex)
        {
            await LogStateErrorsAsync(LocationType.Locations, ex);
            throw;
        }
    }

    private async Task<IPaginatedModel<LocationsDataStoreModel>> GetLocationsAsync(int pageNo = 1)
    {
        var uriBuilder = new UriBuilder(_coreLocationsUrl!)
        {
            Path = $"production/v1/{_tenant.TenantId}/locations",
            Query = $"pageSize={_pageSize}&page={pageNo}"
        };
        var httpClient = _httpClientFactory.CreateClient(nameof(LocationsDataStoreModel));

        return await GetDataFromApiAsync<LocationsDataStoreModel>(uriBuilder, httpClient);
    }

    public async Task<IPaginatedModel<T>> GetDataFromApiAsync<T>(UriBuilder uriBuilder, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(uriBuilder.ToString());
        return await response.Content.ReadFromJsonAsync<PaginatedStoreModel<T>>() ??
               throw new UnprocessableEntityException();
    }

    private async Task LogStateErrorsAsync(LocationType changeTableType, Exception ex)
    {
        await LogStateErrorsAsync(changeTableType.ToString(), ex);
    }

    private async Task LogStateErrorsAsync(string changeType, Exception ex)
    {
        await _aggregateData.UpdateStateAsync(
            StateEventType.CycleError,
            true,
            ex.ToString());

        Log.Logger.Error(
            "Error inserting {S}{FullMessage}",
            changeType,
            ex.ToString());
    }
}
