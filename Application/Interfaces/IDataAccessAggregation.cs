using Domain.Enum;
using Domain.Interfaces;
using Persistence.Kx.Availability.Data.Mongo.Abstractions;


namespace Application.Interfaces;

public interface IDataAccessAggregation : IDataAccess
{
    Task DeleteAsync(IDataModel? data);
    Task InsertAsync(IDataModel data);    
    Task UpdateAsync();
    
    Task UpdateStateAsync(StateEventType state, bool isCompleted = false, string? exception = null);
    Task InsertStateAsync(ITenantDataModel stateRecord);
    Task<int> CountAsync();    
    void StartStateRecord();
    HasPreviousRunEndedEnum HasPreviousRunEnded();
}
