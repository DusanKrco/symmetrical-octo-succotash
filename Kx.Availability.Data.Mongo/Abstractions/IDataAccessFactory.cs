using Domain.Interfaces;

namespace Persistence.Kx.Availability.Data.Mongo.Abstractions;

public interface IDataAccessFactory
{
    IDataAccess GetDataAccess(KxDataType kxDataType);
    IDataAggregationStoreAccess<T> GetDataStoreAccess<T>() where T : class;
}