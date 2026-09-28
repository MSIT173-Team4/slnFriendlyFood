
#nullable disable
using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TEmailVerification
{
    public int FId { get; set; }

    public int FUserId { get; set; }

    public string FToken { get; set; }

    public string FType { get; set; }

    public DateTime FExpireAt { get; set; }

    public bool FUsed { get; set; }

    public virtual TUser FUser { get; set; }
}