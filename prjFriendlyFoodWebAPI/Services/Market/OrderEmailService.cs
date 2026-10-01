using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Market;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Member;

namespace prjFriendlyFoodWebAPI.Services.Market
{
    public interface IOrderEmailService
    {
        Task SendPaymentSuccessAsync(long batchId);
    }

    public class OrderEmailService : IOrderEmailService
    {
        private readonly FriendlyFoodDbContext _context;
        private readonly IOrderQueryService _orderQuery;
        private readonly EmailServices _email;
        private readonly IConfiguration _config;

        public OrderEmailService(
            FriendlyFoodDbContext context,
            IOrderQueryService orderQuery,
            EmailServices email,
            IConfiguration config)
        {
            _context = context;
            _orderQuery = orderQuery;
            _email = email;
            _config = config;
        }

        public async Task SendPaymentSuccessAsync(long batchId)
        {
            // 買家的 Email 與姓名
            var buyer = await _context.TMarketCheckoutBatches
                .AsNoTracking()
                .Where(b => b.FBatchId == batchId)
                .Select(b => new { b.FUser.FEmail, b.FUser.FLastName, b.FUser.FFirstName })
                .FirstOrDefaultAsync();

            if (buyer == null || string.IsNullOrWhiteSpace(buyer.FEmail))
                return;

            // 系統內部呼叫（付款 callback），不檢查擁有者
            var order = await _orderQuery.GetOrderCompleteAsync(batchId, null);
            if (order == null) return;

            var buyerName = $"{buyer.FLastName}{buyer.FFirstName}".Trim();
            var link = $"{_config["AngularBaseUrl"]}/checkout/complete/{batchId}";
            var body = BuildBody(order, string.IsNullOrEmpty(buyerName) ? "會員" : buyerName, link);

            await _email.SendEmailAsync(
                buyer.FEmail,
                $"【友料美食】付款成功，訂單已成立（{order.BatchNo}）",
                body);
        }

        // 使用者輸入的內容（商品名稱、地址等）一律編碼，避免破壞信件版面或被注入 HTML
        private static string E(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);
        private static string Money(decimal value) => $"NT$ {value:N0}";

        // 信件 HTML：多數郵件軟體不支援 <style> 與外部 CSS，所以全部用 table + inline style
        private static string BuildBody(OrderCompleteDto o, string buyerName, string link)
        {
            var sb = new StringBuilder();

            sb.Append($@"
                        <div style=""background:#fdf7f4;padding:24px 0;font-family:'Microsoft JhengHei',Arial,sans-serif;color:#2b1b14;"">
                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0""
                               style=""max-width:640px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;"">
                          <tr><td style=""background:#832600;padding:24px;color:#ffffff;"">
                            <div style=""font-size:14px;opacity:.85;"">友料美食生活平台 FriendlyFood</div>
                            <div style=""font-size:22px;font-weight:700;margin-top:6px;"">付款成功，訂單已成立</div>
                          </td></tr>
                          <tr><td style=""padding:24px;"">
                            <p style=""margin:0 0 8px;"">親愛的 {E(buyerName)} 您好：</p>
                            <p style=""margin:0 0 16px;color:#6b5a52;"">感謝您的訂購！我們已收到您的付款，以下是本次的訂單明細。</p>
                            <p style=""margin:0;font-size:13px;color:#6b5a52;"">
                              結帳編號：{E(o.BatchNo)}　付款時間：{o.PaidAt:yyyy/MM/dd HH:mm}
                            </p>
                          </td></tr>");

            // 每個賣家一個區塊：品項 + 收件資訊 + 本訂單金額
            foreach (var g in o.OrderGroups)
            {
                sb.Append($@"
                      <tr><td style=""padding:0 24px 16px;"">
                        <div style=""border:1px solid #f0e2dc;border-radius:10px;overflow:hidden;"">
                          <div style=""background:#fff4ec;padding:12px 16px;font-weight:700;"">
                            {E(g.SellerName)}
                            <span style=""float:right;font-weight:400;font-size:12px;color:#6b5a52;"">訂單 {E(g.OrderNo)}</span>
                          </div>
                          <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size:14px;"">");

                foreach (var i in g.Items)
                {
                    sb.Append($@"
                            <tr>
                              <td style=""padding:10px 16px;border-top:1px solid #f5ebe6;"">
                                {E(i.ProductName)}
                                <div style=""font-size:12px;color:#8a7a72;"">{Money(i.UnitPrice)} × {i.Quantity}</div>
                              </td>
                              <td align=""right"" style=""padding:10px 16px;border-top:1px solid #f5ebe6;white-space:nowrap;"">
                                {Money(i.LineTotal)}
                              </td>
                            </tr>");
                }

                sb.Append($@"
                          </table>
                          <div style=""padding:12px 16px;background:#fcfaf9;font-size:13px;color:#6b5a52;line-height:1.8;"">
                            收件人：{E(g.RecipientName)}（{E(g.RecipientPhone)}）<br/>
                            收件地址：{E(g.ShippingAddress)}<br/>
                            本訂單金額：<b style=""color:#2b1b14;"">{Money(g.OrderAmount)}</b>
                          </div>
                        </div>
                      </td></tr>");
            }

            // 結帳金額總覽（順序與完成頁一致）
            sb.Append(@"
                      <tr><td style=""padding:8px 24px 24px;"">
                        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""font-size:14px;"">");

                                void Row(string label, string value, string color = "#2b1b14") =>
                                    sb.Append($@"
                          <tr>
                            <td style=""padding:6px 0;color:#6b5a52;"">{label}</td>
                            <td align=""right"" style=""padding:6px 0;color:{color};"">{value}</td>
                          </tr>");

                                Row("商品小計", Money(o.SubTotal));
                                if (o.ProductDiscount > 0) Row("優惠折抵", "－ " + Money(o.ProductDiscount), "#406840");
                                Row("運費", Money(o.ShippingFee));
                                if (o.ShippingDiscount > 0) Row("運費折抵", "－ " + Money(o.ShippingDiscount), "#406840");
                                Row("付款方式", "信用卡一次付清");

                                sb.Append($@"
                          <tr>
                            <td style=""padding:12px 0 0;border-top:1px solid #f0e2dc;font-weight:700;"">實付結帳總額</td>
                            <td align=""right"" style=""padding:12px 0 0;border-top:1px solid #f0e2dc;font-size:20px;font-weight:700;color:#832600;"">
                              {Money(o.TotalAmount)}
                            </td>
                          </tr>
                        </table>
                      </td></tr>
                      <tr><td align=""center"" style=""padding:0 24px 32px;"">
                        <a href=""{E(link)}""
                           style=""display:inline-block;background:#832600;color:#ffffff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;"">
                          查看訂單明細
                        </a>
                      </td></tr>
                      <tr><td style=""padding:16px 24px;background:#fcfaf9;font-size:12px;color:#8a7a72;text-align:center;"">
                        此信件由系統自動發送，請勿直接回覆。
                      </td></tr>
                    </table>
                    </div>");

            return sb.ToString();
        }
    }
}