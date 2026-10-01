namespace prjFriendlyFoodWebAPI.Services.ImageUpload
{
    // 資料夾名稱集中管理，避免各處手打字串打錯，圖片被分散到不同資料夾
    public static class CloudinaryFolders
    {
        public const string Products = "friendlyfood/products";
        public const string Avatars = "friendlyfood/avatars";
        public const string Recipes = "friendlyfood/recipes";
    }
}
