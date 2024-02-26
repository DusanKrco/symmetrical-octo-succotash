using Application.Dto;
using Application.Interfaces;
using Domain.Enum;
using Domain.Interfaces;
using Domain.Models;
using Domain.StoredModels;
using Kx.Core.Common.Exceptions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Persistence.Kx.Availability.Data.Mongo.Abstractions;
using Serilog;
using System.Data;
using System.Net;
using System.Net.Http.Json;

//using Kx.Core.Common.Interfaces;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable PossibleMultipleEnumeration

namespace Application;

public class DataAggregationService : IDataAggregationService
{
    private readonly ITenant _tenant;
    private readonly IRoomService _roomService;
    private readonly ILocationService _locationService;      
    private readonly IDataAccessAggregation _aggregateData;    
    private readonly IDataAggregationStoreAccess<LocationsDataStoreModel> _locationsData;
    private readonly IDataAggregationStoreAccess<BedroomsDataStoreModel> _roomsData;    
    private readonly string? _mongoId;         
        

    public DataAggregationService(IDataAccessFactory dataAccessFactory, ITenant tenant, IConfiguration config,
        IRoomService roomService, ILocationService locationService)
    {

        _tenant = tenant;                       

        var dbAccessAggregate = dataAccessFactory.GetDataAccess(KxDataType.AvailabilityAggregation);
        _aggregateData = DataAccessHelper.ParseAggregationDataAccess(dbAccessAggregate);

        _locationsData = dataAccessFactory.GetDataStoreAccess<LocationsDataStoreModel>();
        _roomsData = dataAccessFactory.GetDataStoreAccess<BedroomsDataStoreModel>();
     
        _roomService = roomService;
        _locationService = locationService;

        _mongoId = config.GetSection("MongoID").Value ?? null;             
    }

    private async Task CreateIndexes()
    {
        await _locationService.CreateLocationsIndexes();
        await _roomService.CreateRoomsIndexes();
    }

    public async Task<(HttpStatusCode statusCode, string result)> ReloadOneTenantsDataAsync()
    {
        try
        {            
            
            _aggregateData.StartStateRecord();
            
            Log.Information("Cleaning tmp table");
            await CleanTenantTempTablesAsync();
            
            await CreateIndexes();
                                    
            //1. Get Locations 
            var locationsTask = _locationService.DoLocationsAsync();
                                    
            //2. Get the rooms
            var roomsTask = _roomService.DoRoomsAsync();
            
            await Task.WhenAll(locationsTask, roomsTask);
            
            //3. Mash them together
            //make the main table from all imported tables            
            await MashTempTablesIntoTheAvailabilityModelAsync();
            
            //4. save tenantAvailabilityModel.            
            await MoveTempTenantToLive();                        
            await CleanTenantTempTablesAsync();
                        
            return (HttpStatusCode.NoContent, string.Empty);
            
        }
        catch (Exception ex)
        {
            return (HttpStatusCode.ExpectationFailed, ex.Message);
        }
    }

    private async Task MashTempTablesIntoTheAvailabilityModelAsync()
    {
        try
        {
            
            var aggregatedAvailabilityModel = GetAggregatedDataStoreModel();

            var rooms = _roomsData.QueryFreely();

            if (rooms is null || !rooms.Any()) throw new DataException();                                   
            
            foreach (var room in rooms)
            {                                
                var availabilityModel = CreateAvailabilityMongoModel();
                availabilityModel.ID = _mongoId;
                if (_mongoId is null)
                {
                    availabilityModel.ID = Convert.ToString(availabilityModel.GenerateNewID());
                }

                availabilityModel.TenantId = _tenant.TenantId;
                availabilityModel.RoomId = room.RoomId;
                
                var addLocations = _locationService.AddLocationModels(room);
                availabilityModel.Locations.AddRange(addLocations);
                
                aggregatedAvailabilityModel.Availability.Add(availabilityModel);
            }
          
            await _aggregateData.InsertAsync(aggregatedAvailabilityModel);
          
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to mash data together: {ex}");
            throw;
        }
    }    

    private AvailabilityMongoModel CreateAvailabilityMongoModel()
    {
        return new AvailabilityMongoModel
        {
            TenantId = _tenant.TenantId,
            RoomId = string.Empty, 
            Locations = new List<LocationModel>()
        };
    }

    private AggregatedAvailabilityModel GetAggregatedDataStoreModel()
    {
        var data = new AggregatedAvailabilityModel
        {
            TenantId = _tenant.TenantId
        };
        return data;
    }  

    public async Task InsertStateAsync(ITenantDataModel item)
    {
        await _aggregateData.InsertStateAsync(item);
    }

    public async Task<IPaginatedModel<T>> GetDataFromApiAsync<T>(UriBuilder uriBuilder, HttpClient httpClient)
    {
        var response = await httpClient.GetAsync(uriBuilder.ToString());
        return await response.Content.ReadFromJsonAsync<PaginatedStoreModel<T>>() ??
               throw new UnprocessableEntityException();
    }

    private async Task MoveTempTenantToLive()
    {
        await _aggregateData.UpdateAsync();
    }

    private async Task CleanTenantTempTablesAsync()
    {
        await _locationsData.DeleteAsync();
        await _roomsData.DeleteAsync();        
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
