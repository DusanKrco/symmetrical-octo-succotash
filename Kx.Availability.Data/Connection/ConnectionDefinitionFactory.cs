using Kx.Core.Common.Data;
using Kx.Core.Common.Data.MongoDB;
using Persistence.Kx.Availability.Data.Mongo.Data;

namespace Kx.Availability.Data.Connection;

public class ConnectionDefinitionFactory : IConnectionDefinitionFactory
{

    private readonly IMongoSettings _mongoSettings;    

    public ConnectionDefinitionFactory (IMongoSettings mongoSettings)
    {
        _mongoSettings = mongoSettings;        
    }    

    public IMongoDbConnection GetMongoDbConnection()
    {
        return new MongoDbConnection(_mongoSettings);
    }
}