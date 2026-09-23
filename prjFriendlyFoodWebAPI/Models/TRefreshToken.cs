using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TRefreshToken
{
    public int FId { get; set; }

    public int FUserId { get; set; }

    public string FToken { get; set; } = null!;

    public DateTime FCreate { get; set; }

    public DateTime FExpired { get; set; }

    public bool FRevoke { get; set; }

    public virtual TUser FUser { get; set; } = null!;
}
