namespace prjFriendlyFoodWebAPI.Models
{
    public partial class TExternalLogin
    {
        public int FId { get; set; }

        public int FUserId { get; set; }

        public string FProvider { get; set; } = null!;

        public string FProviderUserId { get; set; } = null!;

        public virtual TUser FUser { get; set; } = null!;
    }
}
