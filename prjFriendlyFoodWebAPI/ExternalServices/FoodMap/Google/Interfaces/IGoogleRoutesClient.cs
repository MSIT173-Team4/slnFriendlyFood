namespace prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces
{
    public interface IGoogleRoutesClient
    {
        Task<GoogleRouteResult> ComputeRouteAsync(
                List<(double Latitude, double Longitude)> waypoints,
                CancellationToken cancellationToken = default
            );
            
    }

    public class GoogleRouteResult
    {
       public int TotalDistanceMeters { get; set; }
       
        public int TotalDurationSeconds { get; set; }

        public string EncodedPolyline { get; set; } = string.Empty;

        public List<GoogleRouteLeg> Legs { get; set; } = [];

    }

    public class GoogleRouteLeg
    {
        public int DistanceMeters { get; set; }
        public int DurationSeconds { get; set; }
        public string EncodedPolyline { get; set; } = string.Empty;
    }
}

