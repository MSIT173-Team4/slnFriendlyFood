using System.Security.Claims;

namespace prjFriendlyFoodWebAPI.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        // 需登入的 API 用：有 [Authorize] 保證一定有 claim
        // 取法與會員模組 CurrentUser / Apply 一致（ClaimTypes.NameIdentifier）
        public static int GetUserId(this ClaimsPrincipal user)
        {
            return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // 不強制登入的 API 用：沒登入回傳 null
        public static int? TryGetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
