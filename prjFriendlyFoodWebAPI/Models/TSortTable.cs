using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Models;

public partial class TSortTable
{
    public int FSortId { get; set; }

    public string FSortName { get; set; } = null!;

    public virtual ICollection<TPostTable> TPostTables { get; set; } = new List<TPostTable>();
}
