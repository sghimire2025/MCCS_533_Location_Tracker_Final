using LocationTrackerFinal.Models;

namespace LocationTrackerFinal.Services
{
    public interface IHeatMapService
    {
        Task<List<HeatMapPoint>> GenerateHeatMapDataAsync();
        Task<List<HeatMapPoint>> GenerateHeatMapDataForSessionAsync(int sessionId);
        
        // Crowd simulation methods
        Task<List<HeatMapPoint>> GenerateHeatMapDataWithCrowdAsync();
        Task<List<HeatMapPoint>> GenerateHeatMapDataForSessionWithCrowdAsync(int sessionId);
        void SetCrowdSimulationEnabled(bool enabled);
        bool IsCrowdSimulationEnabled { get; }
    }
}
