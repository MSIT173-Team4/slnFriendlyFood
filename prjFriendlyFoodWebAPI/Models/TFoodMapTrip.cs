using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapTrip
{
    public int FTripId { get; set; }

    public int? FUserId { get; set; }

    public string FTripName { get; set; } = null!;

    public DateOnly? FTripDate { get; set; }

    public TimeOnly? FStartTime { get; set; }

    public string? FDescription { get; set; }

    public string FStatus { get; set; } = null!;

    public DateTime FCreatedTime { get; set; }

    public DateTime? FUpdatedTime { get; set; }

    public virtual TUser? FUser { get; set; }

    public virtual ICollection<TFoodMapTripPlace> TFoodMapTripPlaces { get; set; } = new List<TFoodMapTripPlace>();

    public virtual ICollection<TFoodMapTripRoute> TFoodMapTripRoutes { get; set; } = new List<TFoodMapTripRoute>();
}
