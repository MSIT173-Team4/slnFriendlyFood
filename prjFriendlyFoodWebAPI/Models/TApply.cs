using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TApply
{
    public int FId { get; set; }

    public int FUserId { get; set; }

    public string FStoreName { get; set; } = null!;

    public string? FStoreDescription { get; set; }

    public string FIdNum { get; set; } = null!;

    public string FIdCard { get; set; } = null!;

    public int FStatus { get; set; }

    public DateTime FSendingDate { get; set; }

    public virtual TApplyStatus FStatusNavigation { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;
}
