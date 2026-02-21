using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Services
{
    public interface IHeatMapService
    {
        Task<List<HeatMapPoint>> GenerateHeatMapDataAsync();
        Task<List<HeatMapPoint>> GenerateHeatMapDataForSessionAsync(int sessionId);
    }
}
