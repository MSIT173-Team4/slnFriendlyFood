using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Model;

public partial class TStatus
{
    public int FId { get; set; }

    public string FStatusName { get; set; } = null!;

    public virtual ICollection<TSeller> TSellers { get; set; } = new List<TSeller>();
}
