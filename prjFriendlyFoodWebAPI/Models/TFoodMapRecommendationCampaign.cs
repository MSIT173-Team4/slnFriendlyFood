using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapRecommendationCampaign
{
    public int FCampaignId { get; set; }

    public string FTitle { get; set; } = null!;

    public string? FContent { get; set; }

    public string? FBannerImageUrl { get; set; }

    public int FPriority { get; set; }

    public DateOnly? FStartDate { get; set; }

    public DateOnly? FEndDate { get; set; }

    public bool FIsActive { get; set; }

    public DateTime FCreatedTime { get; set; }

    public DateTime? FUpdatedTime { get; set; }

    public virtual ICollection<TFoodMapRecommendationPlace> TFoodMapRecommendationPlaces { get; set; } = new List<TFoodMapRecommendationPlace>();
}
