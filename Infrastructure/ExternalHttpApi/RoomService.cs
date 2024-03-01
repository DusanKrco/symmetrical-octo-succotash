using Application.Dto;
using Application.Helpers;
using Domain.Abstractions;
using Domain.Enum;
using Domain.Models;
using Domain.StoredModels;
using Kx.Core.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Persistence.Kx.Availability.Data.Mongo.Abstractions;
using Serilog;
using System.Net.Http.Json;

namespace Infrastructure.ExternalHttpApi
{
    public class RoomService : IRoomService
    {
        private readonly IDataAggregationStoreAccess<BedroomsDataStoreModel> _roomsData;
        private readonly string? _coreBedroomsUrl;
        private readonly ITenant _tenant;
        private readonly int _pageSize;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDataAccessAggregation _aggregateData;

        public RoomService(IDataAccessFactory dataAccessFactory, IDataAggregationStoreAccess<BedroomsDataStoreModel> roomData, IConfiguration config,
            ITenant tenant, IHttpClientFactory httpClientFactory)
        {
            _tenant = tenant;
            _httpClientFactory = httpClientFactory;

            var dbAccessAggregate = dataAccessFactory.GetDataAccess(KxDataType.AvailabilityAggregation);
            _aggregateData = DataAccessHelper.ParseAggregationDataAccess(dbAccessAggregate);

            _roomsData = roomData;
            _coreBedroomsUrl = config.GetSection("BEDROOMS_URL").Value;

            _pageSize = 1000;
            if (int.TryParse(config.GetSection("DEFAULT_PAGE_SIZE").Value, out var pageSize))
            {
                _pageSize = pageSize;
            }
        }

        public async Task CreateRoomsIndexes()
        {
            var indexBuilder = Builders<BedroomsDataStoreModel>.IndexKeys;
            var indexModel = new CreateIndexModel<BedroomsDataStoreModel>(indexBuilder.Ascending(x => x.RoomId));
            await _roomsData.AddIndex(indexModel);
        }

        public async Task DoRoomsAsync()
        {
            try
            {
                var pageOfRooms = await GetRoomsFromBedroomsApiAsync();
                await _roomsData.InsertPageAsync<PaginatedStoreModel<BedroomsDataStoreModel>>(pageOfRooms);


                if (pageOfRooms.TotalPages > 1)
                {

                    for (var i = 2; i <= pageOfRooms.TotalPages; i++)
                    {

                        var page = await GetRoomsFromBedroomsApiAsync(i);
                        await _roomsData.InsertPageAsync<PaginatedStoreModel<BedroomsDataStoreModel>>(page);
                    }
                }
            }
            catch (Exception ex)
            {
                await LogStateErrorsAsync(LocationType.Rooms, ex);
                throw;
            }
        }

        private async Task<IPaginatedModel<BedroomsDataStoreModel>> GetRoomsFromBedroomsApiAsync(int pageNo = 1)
        {
            var uriBuilder = new UriBuilder(_coreBedroomsUrl!)
            {
                Path = $"production/v1/{_tenant.TenantId}/bedrooms/rooms",
                Query = $"pageSize={_pageSize}&page={pageNo}"
            };
            var httpClient = _httpClientFactory.CreateClient(nameof(BedroomsDataStoreModel));

            return await GetDataFromApiAsync<BedroomsDataStoreModel>(uriBuilder, httpClient);
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
}
