using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Kx.Availability.Controllers
{
    [Route("bedroom-availability")]
    public class AvailabilityController : ControllerBase
    {
        private readonly IAvailabilityService _availabilityService;
        public AvailabilityController(IAvailabilityService availabilityService)
        {
           _availabilityService = availabilityService;
        }

        [HttpPost]
        public async Task<IResult> ReloadOneTenantsData()
        {
            return await _availabilityService.ReloadOneTenantsData();
        }
    }
}
