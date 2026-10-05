using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    // 加入購物車的結果；失敗時 Message 是給使用者看的原因
    public record CartAddResult(bool Success, string? Message = null);

    public interface ICartService
    {
        // 加入購物車（只修改 DbContext，不存檔，由呼叫端決定何時 SaveChanges）
        Task<CartAddResult> AddAsync(int userId, int productId, int quantity);

        // 再買一次：把指定訂單的商品加回購物車，能加的加、不能加的回傳原因
        Task<RebuyResultDto> RebuyAsync(int userId, IEnumerable<long> orderIds);
    }

    public class CartService : ICartService
    {
        private readonly FriendlyFoodDbContext _context;

        public CartService(FriendlyFoodDbContext context)
        {
            _context = context;
        }

        public async Task<CartAddResult> AddAsync(int userId, int productId, int quantity)
        {
            if (quantity <= 0)
                return new CartAddResult(false, "數量必須大於 0");

            var product = await _context.TMarketProducts
                .FirstOrDefaultAsync(p => p.FProductId == productId);

            if (product == null)
                return new CartAddResult(false, "商品不存在");

            if (product.FProductStatus == ProductStockRules.SoldOut || product.FStock <= 0)
                return new CartAddResult(false, "已售完");

            if (product.FProductStatus != ProductStockRules.OnSale)
                return new CartAddResult(false, "已下架");

            var isOwnProduct = await _context.TSellers
                .AnyAsync(s => s.FId == product.FSellerId && s.FUserId == userId);
            if (isOwnProduct)
                return new CartAddResult(false, "不能購買自己上架的商品");

            var existing = await _context.TMarketShoppingCarts
                .FirstOrDefaultAsync(c => c.FUserId == userId && c.FProductId == productId);

            int newQty = (existing?.FQuantity ?? 0) + quantity;
            if (newQty > product.FStock)
                return new CartAddResult(false, $"庫存不足（目前庫存 {product.FStock}）");

            if (existing != null)
            {
                existing.FQuantity = newQty;
            }
            else
            {
                _context.TMarketShoppingCarts.Add(new TMarketShoppingCart
                {
                    FUserId = userId,
                    FSellerId = product.FSellerId,
                    FProductId = productId,
                    FQuantity = quantity
                });
            }

            return new CartAddResult(true);
        }

        public async Task<RebuyResultDto> RebuyAsync(int userId, IEnumerable<long> orderIds)
        {
            var ids = orderIds.Distinct().ToList();

            // 只取自己的訂單明細；同一商品出現在多張訂單時合併數量
            var lines = await _context.TMarketOrderDetails
                .Where(d => ids.Contains(d.FOrderId) && d.FOrder.FUserId == userId)
                .GroupBy(d => new { d.FProductId, d.FProduct.FProductName })
                .Select(g => new
                {
                    g.Key.FProductId,
                    g.Key.FProductName,
                    Quantity = g.Sum(d => d.FQuantity)
                })
                .ToListAsync();

            var result = new RebuyResultDto();

            foreach (var line in lines)
            {
                var add = await AddAsync(userId, line.FProductId, line.Quantity);
                if (add.Success)
                    result.AddedCount++;
                else
                    result.Skipped.Add(new RebuySkippedItemDto
                    {
                        ProductName = line.FProductName,
                        Reason = add.Message ?? "無法加入"
                    });
            }

            // 全部處理完才一次存檔
            await _context.SaveChangesAsync();
            return result;
        }
    }
}