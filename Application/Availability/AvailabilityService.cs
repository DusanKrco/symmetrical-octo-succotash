using Application.Interfaces;
using Kx.Core.Common.HelperClasses;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Application.Availability
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IDataAggregationService _dataAggregationService;
        public AvailabilityService(IDataAggregationService dataAggregationService)
        {
            _dataAggregationService = dataAggregationService;
        }

        public async Task<IResult> ReloadOneTenantsData()
        {
            try
            {
                var results = await _dataAggregationService.ReloadOneTenantsDataAsync();
                return ReturnResults.Result(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed reload tenants data");
                return Results.Problem(ex.ToString());
            }
        }
    }
}
