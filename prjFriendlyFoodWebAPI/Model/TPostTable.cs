using System;
using System.Collections.Generic;

namespace prjFriendlyFoodWebAPI.Model;

public partial class TPostTable
{
    public int FPostId { get; set; }

    public int FUserId { get; set; }

    public string FTitle { get; set; } = null!;

    public int FLikes { get; set; }

    public int FViews { get; set; }

    public DateTime FPostDate { get; set; }

    public byte FPostState { get; set; }

    public int FSortId { get; set; }

    public int? FRecipeId { get; set; }

    public virtual TRecipe? FRecipe { get; set; }

    public virtual TSortTable FSort { get; set; } = null!;

    public virtual TUser FUser { get; set; } = null!;

    public virtual ICollection<TMessageTable> TMessageTables { get; set; } = new List<TMessageTable>();

    public virtual ICollection<TPostBlockTable> TPostBlockTables { get; set; } = new List<TPostBlockTable>();

    public virtual ICollection<TUser> FUsers { get; set; } = new List<TUser>();
}
