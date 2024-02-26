using Microsoft.AspNetCore.Http;

namespace Application.Interfaces
{
    public interface IAvailabilityService
    {
        public Task<IResult> ReloadOneTenantsData();
    }
}
