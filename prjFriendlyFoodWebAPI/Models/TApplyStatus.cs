using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TApplyStatus
{
    public int FId { get; set; }

    public string FApplyStatusName { get; set; } = null!;

    public virtual ICollection<TApply> TApplies { get; set; } = new List<TApply>();
}
