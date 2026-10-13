using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using prjFriendlyFoodWebAPI.DTOs.Social;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Social;
using System.Security.Claims;

//namespace 專案名稱.控制器.功能分類
namespace prjFriendlyFoodWebAPI.Controllers.Forum
{
    //接收 HTTP Request、取得參數、呼叫 Service、回傳 HTTP Response。
    //取得貼文列表、單一貼文資訊，建立、更新、刪除貼文，以及處理貼文的收藏和按讚功能。
    //[...]為屬性標註
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        //依賴注入（Dependency Injection，DI）
        //_postService：欄位名稱，依慣例使用底線開頭，代表私有欄位。
        private readonly IPostService _postService;

        //建構子
        //接收 IPostService 介面實作的物件。
        public PostController(IPostService postService)
        {
            _postService = postService;
        }
        //jwt 驗證後，User 物件會包含使用者的身分資訊（Claims）。
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        //取得貼文列表
        //明確指定 API 回傳的資料型別，使用ActionResult
        /*PagedResultDto<PostResponseDto>回傳分頁資訊，
         * 目前取得哪些貼文、貼文總數及目前頁數*/
        //[FromQuery]：從 URL 查詢字串取得頁籤、關鍵字、頁數、頁數大小等參數。
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<PostResponseDto>>> GetPosts([FromQuery] PostQueryParameters queryParams)
        {
            int currentUserId = GetCurrentUserId();
            var result = await _postService.GetPostsAsync(queryParams, currentUserId);
            return Ok(result);
        }

        //GET /Post/{id} 設定路由參數
        //以id取得單一貼文資訊
        //狀態碼可決定網站接下來的應對措施，也方便除錯
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPost(int id)
        {
            int currentUserId = GetCurrentUserId();
            var post = await _postService.GetPostByIdAsync(id, currentUserId);
            if (post == null) return NotFound();
            return Ok(post);
        }

        //接收前端送來的貼文資料，交給 Service 建立貼文，成功時回傳新貼文的 ID。
        //POST 常用於建立新資源，或用於執行不適合單純用 GET 表達的操作。
        /*使用 [FromBody] 讓 ASP.NET Core 從 HTTP Request 的 Body 讀取 JSON 資料，
         * 並自動反序列化成 CreateOrUpdatePostDto 物件。*/
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreateOrUpdatePostDto dto)
        {

            try
            {
                int currentUserId = GetCurrentUserId();
                var postId = await _postService.CreatePostAsync(dto, currentUserId);
                return Ok(new { postId });
            }
            catch (Exception ex)
            {
                //將詳細的錯誤訊息傳給前端顯示
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = errorMsg, detail = ex.ToString() });
                /*如果 ex.InnerException != null，就使用 ex.InnerException.Message。
                 * 否則，使用 ex.Message。*/
                //ex.InnerException: 目前例外內部包裝的另一個例外，可能是更底層的錯誤
                //ex.ToString(): 例外的完整字串表示，包含堆疊追蹤資訊，方便除錯
                /*這樣前端就可以讀取錯誤訊息，例如：
                    const data = await response.json();

                    console.log(data.message);
                    console.log(data.detail);*/
            }
        }
        //HttpPut: 用於更新既有資源。
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] CreateOrUpdatePostDto dto)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var updateSuccess = await _postService.UpdatePostAsync(id, dto, currentUserId);
            if (!updateSuccess) return NotFound();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var deleteSuccess = await _postService.DeletePostAsync(id, currentUserId);
            if (!deleteSuccess) return NotFound();
            return Ok();
        }

        [HttpPost("bookmark")]
        public async Task<ActionResult<PostBookmarkStatusDto>> ToggleBookmark([FromBody] BookmarkCreateDto dto)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            var result = await _postService.ToggleBookmarkAsync(dto.PostId, currentUserId);
            return Ok(result);
        }

        [HttpPost("{postId}/like")]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized("尚未登入");

            bool isLiked = await _postService.ToggleLikeAsync(postId, currentUserId);
            return Ok(new { postId, isLiked });
        }
    }
}

/*[ApiController]: C#的Attribute（屬性標記），類別是 Web API Controller，並啟用一些 API 常用行為。
     * 常見的效果包括：
     * 自動推斷部分參數的繫結來源。
     * 對模型驗證錯誤進行自動處理，符合條件時會回傳 400 Bad Request。
     * 提供更符合 API 使用情境的參數繫結行為。
     * 例如，當 DTO 有設定必要欄位的驗證規則，
     * 收到不符合規則的資料時，框架可能會在進入 Action 方法前就回傳 400。
     * Web API通常接收JSON、回傳JSON，不需要傳統 MVC 網頁的 View。
     * [ApiController] 能減少一些重複的驗證與參數處理程式碼。*/

/*[Route("api/[controller]")]這是設定 Controller 的路由規則。
 * api/：固定的網址前綴。
 * [controller]：由 Controller 類別名稱推斷，會移除名稱結尾的 Controller。
public class PostController : ControllerBase
會對應到：/api/Post
[controller] 的好處是，不必在每個 Controller 都手動寫出類別名稱。*/

/*public class PostController : ControllerBase 為什麼要繼承 ControllerBase？
 * 因為它提供 Web API 常用的功能，例如：
 * return Ok(result);
 * return NotFound();
 * return Unauthorized();
 * return StatusCode(500);
這些方法可以產生對應的 HTTP 回應。
如果是只負責 Web API 的 Controller，通常使用 ControllerBase 就足夠；
如果還需要 MVC View 等網頁功能，才會考慮使用 Controller。*/

/*介面與實作分離，可以降低 Controller 對具體類別的依賴
 IPostService定義，PostService 再負責實作這個方法
Controller 只需要知道介面提供什麼功能，不必知道具體實作的內部細節。
這也讓單元測試更容易替換成模擬的 Service*/

/*專案需要在 DI 容器中註冊 IPostService 的實作，
 * 例如在 Program.cs 中：
 * builder.Services.AddScoped<IPostService, PostService>();
AddScoped 表示在同一個 HTTP 請求的服務範圍內，共用該服務的實例。
實際註冊方式仍要以專案設定為準。*/

/*var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 * 這行有幾個重點：
 * User：目前 HTTP 請求所關聯的使用者身分資訊，型別為 ClaimsPrincipal。
 * FindFirst(...)：尋找指定類型的第一個 Claim。
 * ClaimTypes.NameIdentifier：表示使用者識別碼的 Claim 類型。
 * ?.Value：如果找得到 Claim，就讀取它的值；如果找不到，就回傳 null，避免因為空值而發生例外。
 * var：讓編譯器自動推斷變數型別。這裡的 userIdClaim 通常是 string 或 null。
什麼是 Claim？
可以把 Claim 理解成使用者身分中的一項資訊，例如：
NameIdentifier = "15"
Name = "Alice"
Role = "Member"
當使用者登入後，驗證機制可能會建立這些身分資訊。
Controller 再從 User 中讀取所需的 Claim。
不過，User 裡是否有 NameIdentifier，取決於登入驗證設定。
若使用 JWT，必須確認 Token 中的 Claim 與驗證後的 Claim 類型有正確對應。*/

/*return int.TryParse(userIdClaim, out int userId)? userId: 0;
 * 條件 ? 條件成立時的結果 : 條件不成立時的結果
 * 嘗試把 userIdClaim 轉成整數。
 * 如果成功，就回傳轉換後的 userId。
 * 如果失敗，例如 Claim 不存在或內容不是數字，就回傳 0。
為什麼使用 TryParse，而不是直接使用 int.Parse？
因為 int.Parse 遇到無效的字串或 null 時，可能會拋出例外；
TryParse 則會回傳 false，讓程式可以自行處理。
if (currentUserId == 0)
    return Unauthorized("尚未登入");
這樣可以讓部分 API 對無法取得使用者 ID 的情況回傳 401 Unauthorized。
更明確的做法也可以是讓方法回傳 int?，
或直接使用 ASP.NET Core 的授權機制限制登入者才能呼叫特定 API。*/

/*[HttpGet]
 * public async Task<ActionResult<PagedResultDto<PostResponseDto>>> GetPosts([FromQuery] PostQueryParameters queryParams)
 * {
 *      int currentUserId = GetCurrentUserId();
 *      var result = await _postService.GetPostsAsync(queryParams, currentUserId);
 *      return Ok(result);
 * }
 * 
 * GET /api/Post，GET 通常用來讀取資料。
 * [FromQuery]：從 URL 查詢字串取得參數。
 * 例如：GET /api/Post?pageNumber=1&pageSize=10&searchTerm=food
 * PostQueryParameters 會自動綁定到 queryParams 物件。
 * ActionResult<PagedResultDto<PostResponseDto>>：回傳分頁結果，包含貼文列表與分頁資訊。*/

/*async: 允許方法使用 await 等待非同步操作
 * Task<T>: 表示方法會非同步地產生一個型別為 T 的結果
 * ActionResult<T>: 可回傳指定型別的資料，也可回傳 HTTP 錯誤等 Action Result
 * PagedResultDto<...>: 泛型型別，表示一份分頁結果
 * PostResponseDto: 描述回傳給前端的單篇貼文資料結構
 * GetPosts: 方法名稱
 * [FromQuery]: 指示從 URL 的 Query String 讀取參數
 * PostQueryParameters queryParams: 接收查詢條件的物件*/

/*什麼是 Task<ActionResult<...>>？
 * 這是一個非同步方法，
 * 執行完成後會回傳一個可以代表 HTTP 回應的結果。
 * Task 不代表背景工作一定會在另一個執行緒執行。
 * 它主要用來表示非同步操作的完成狀態；
 * 像資料庫 I/O 這類操作，通常可以在等待期間釋放執行緒去處理其他請求。*/

/*什麼是 PagedResultDto<PostResponseDto>？
這是一個使用泛型的型別，通常用於包裝分頁資料。
public class PagedResultDto<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
T 是泛型參數。在這個 API 中，T 代入 PostResponseDto，因此 Items 會是貼文 DTO 的集合。
如此一來，前端不只知道目前取得哪些貼文，也可以知道總筆數及目前頁數。*/

/*var result = await _postService.GetPostsAsync(queryParams, currentUserId);
 * queryParams：提供頁籤、關鍵字搜尋或分頁條件。
 * await：等待非同步操作完成。
為什麼不在 Controller 裡直接寫 SQL 查詢？
因為把查詢和業務規則集中在 Service 或資料存取層，
可以避免 Controller 過度膨脹，也方便在其他地方重複使用相同邏輯。*/

/*return Ok(result);
Ok(...) 會建立 HTTP 200 OK 回應，並把 result 作為回應內容。
前端可能收到類似這樣的資料：
json
{
  "items": [
    {
      ...
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 5
}
為什麼使用 Ok(result)，而不是直接回傳字串？
因為 API 不只需要回傳資料，還要清楚表達 HTTP 狀態碼。
使用 Ok() 可以讓框架建立適當的 Action Result。*/

/*為什麼不直接使用一般的 string 接收整份 JSON？
 * 因為 DTO 可以明確定義 API 接受哪些欄位，
 * 也能搭配資料驗證，降低控制器直接處理原始資料的複雜度。*/