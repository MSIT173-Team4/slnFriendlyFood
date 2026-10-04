using prjFriendlyFoodWebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public static class ProductStockRules
    {
        public const byte OnSale = 1;
        public const byte SoldOut = 2;

        // 只在「販售中 ⇄ 已售完」之間自動切換；
        // 審核中、未上架、已違規不受庫存影響（例如草稿就算庫存 0 也維持未上架）
        public static void SyncStatusWithStock(TMarketProduct product)
        {
            if (product.FStock == 0 && product.FProductStatus == OnSale)
                product.FProductStatus = SoldOut;
            else if (product.FStock > 0 && product.FProductStatus == SoldOut)
                product.FProductStatus = OnSale;
        }

        // 結帳扣庫存後使用：這批商品中庫存歸零、仍在販售中的，一次改成已售完
        // （ExecuteUpdate 不經過 EF 追蹤，所以不能用上面的 SyncStatusWithStock，改用同一套規則的 SQL 版）
        public static Task MarkSoldOutAsync(FriendlyFoodDbContext context, IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            return context.TMarketProducts
                .Where(p => ids.Contains(p.FProductId) && p.FStock == 0 && p.FProductStatus == OnSale)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.FProductStatus, SoldOut));
        }
    }
}