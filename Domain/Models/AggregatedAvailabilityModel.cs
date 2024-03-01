using Domain.Abstractions;

namespace Domain.Models;

public class AggregatedAvailabilityModel : ITenantDataModel
{
    public string TenantId { get; set; } = string.Empty;

    public readonly List<AvailabilityMongoModel> Availability = new();
    
    
}