using Application.Interfaces;
using Kx.Core.Common.HelperClasses;
//using Kx.Core.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Kx.Availability.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
