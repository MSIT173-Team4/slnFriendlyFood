using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapTripRoute
{
    public int FTripRouteId { get; set; }

    public int FTripId { get; set; }

    public int? FDistanceMeters { get; set; }

    public int? FDurationSeconds { get; set; }

    public string? FPolyline { get; set; }

    public string? FRouteProvider { get; set; }

    public int? FRouteVersion { get; set; }

    public DateTime FCreatedTime { get; set; }

    public DateTime? FUpdatedTime { get; set; }

    public int? FFromTripPlaceId { get; set; }

    public int? FToTripPlaceId { get; set; }

    public virtual TFoodMapTripPlace? FFromTripPlace { get; set; }

    public virtual TFoodMapTripPlace? FToTripPlace { get; set; }

    public virtual TFoodMapTrip FTrip { get; set; } = null!;
}
