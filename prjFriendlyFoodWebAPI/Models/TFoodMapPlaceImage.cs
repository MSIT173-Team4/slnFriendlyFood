using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TFoodMapPlaceImage
{
    public int FImageId { get; set; }

    public int FPlaceId { get; set; }

    public string FImageUrl { get; set; } = null!;

    public string? FImageType { get; set; }

    public int FSortOrder { get; set; }

    public DateTime FCreatedTime { get; set; }

    public virtual TFoodMapPlace FPlace { get; set; } = null!;
}
