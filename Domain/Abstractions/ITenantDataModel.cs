namespace Domain.Abstractions;

public interface ITenantDataModel : IDataModel
{
    string TenantId { get; set; }
}
