using Domain.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace Domain.Models;

[BsonIgnoreExtraElements]
public class AvailabilityMongoModel : IEntity, ITenantDataModel
{
    public string TenantId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public List<LocationModel> Locations { get; set; } = new();
    
    [BsonId] public string? ID { get; set; }

    public object GenerateNewID() => ObjectId.GenerateNewId().ToString();

    public bool HasDefaultID()
    {
        return false;
    }
}
