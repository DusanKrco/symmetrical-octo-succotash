using Domain.Abstractions;
using Domain.Enum;

namespace Persistence.Kx.Availability.Data.Mongo.Abstractions;

public interface IDataAccessFactory
{
    IDataAccess GetDataAccess(KxDataType kxDataType);
    IDataAggregationStoreAccess<T> GetDataStoreAccess<T>() where T : class;
}