namespace Domain.Interfaces;

public interface ITenantDataModel : IDataModel
{
    string TenantId { get; set; }
}
