using Google.Apis.Auth;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using prjFriendlyFoodWebAPI.DTOs.Member;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Member;
using System.Security.Claims;
namespace prjFriendlyFoodWebAPI.Controllers.Member
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserServices _us;
        private readonly EncodeServices _es;
        private readonly TokenServices _ts;
        private readonly IConfiguration _config;
        private readonly EmailServices _email;
        private readonly IdCardProofingServices _icps;
        public UsersController(IdCardProofingServices icps,EmailServices email, UserServices userServices, EncodeServices encodeServices, TokenServices tokenServices, IConfiguration configuration)
        {
            _icps = icps;
            _email = email;
            _config = configuration;
            _us = userServices;
            _es = encodeServices;
            _ts = tokenServices;
        }


        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserRegisterDTO u)
        {
            if (await _us.IsUsernameExists(u.fUsername))
            {
                return BadRequest(new
                {
                    message = "Username already exists"
                });
            }
            if (await _us.IsEmailExists(u.fEmail))
            {
                return BadRequest(new
                {
                    message = "Email already exists"
                });
            }
            string password = await _es.HashPassword(u.fPassword);
            TUser user = await _us.AddUser(u, password);
            string token=await _ts.GernateTokenString(user.FId, "EmailVerification");
            string verifyUrl =
            $"https://friendlyfood-web-7tsmeq4eta-de.a.run.app/verifyemail?token={token}";

                    string body = $"""
            <h2>FriendlyFood 信箱驗證</h2>

            <p>感謝您註冊 FriendlyFood。</p>

            <p>請點擊下面按鈕完成 Email 驗證：</p>

            <a href="{verifyUrl}">
                驗證 Email
            </a>

            <p>此連結將在 30 分鐘後失效。</p>
            """;
            _email.SendEmailAsync(user.FEmail, "FriendlyFood Email Verification",body 
                );
            return Ok(new
            {
                message = "User registered successfully,Verification mail had sended to your Email."
            });
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginDTO u)
        {



            if (string.IsNullOrEmpty(u.UserName) && string.IsNullOrEmpty(u.Email))
            {
                return BadRequest(new
                {
                    message = "Username or email is required"
                });
            }
            TUser? user = null;
            if (string.IsNullOrEmpty(u.Email))
            {
                if (!await _us.IsUsernameExists(u.UserName))
                {
                    return BadRequest(new { message = "Username or email is not exists" });
                }

                string pass = await _us.GetPasswordByUsername(u.UserName);
                if (!await _es.VerifyPassword(u.Password, pass))
                {
                    return BadRequest(new { message = "Something went wrong,please try again" });
                }

                user = await _us.GetUserByUsername(u.UserName);
            }
            else if (string.IsNullOrEmpty(u.UserName))
            {
                if (!await _us.IsEmailExists(u.Email))
                {
                    return BadRequest(new { message = "Username or email is not exists" });
                }

                string pass = await _us.GetPasswordByEmail(u.Email);
                if (!await _es.VerifyPassword(u.Password, pass))
                {
                    return BadRequest(new { message = "Something went wrong,please try again" });
                }

                user = await _us.GetUserByEmail(u.Email);
            }
            if (user == null)
            {
                return BadRequest(new { message = "Username or email is not exists" });
            }
            if (user.FIsActive == false)
            {
                return BadRequest(new { message = "Account is not active" });
            }
            var token = _ts.GenerateToken(user);
            Response.Cookies.Append("token", await token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(60),
                Path = "/"
            });
            //var refreshToken = _ts.GernateTokenString();
            //Response.Cookies.Append("refreshToken", await refreshToken, new CookieOptions
            //{
            //    HttpOnly = true,
            //    Secure = true,
            //    SameSite = SameSiteMode.None,
            //    Expires = DateTime.UtcNow.AddDays(7),
            //    Path = "/"
            //});
            return Ok(new
            {
                message = "Login success",
            });
        }
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Append("token", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });

            return Ok(new
            {
                message = "Logout success"
            });
        }
        [HttpGet("CheckAuth")]
        [Authorize]
        public IActionResult CheckAuth()
        {
            return Ok(new
            {
                authenticated = true
            });
        }
        [HttpGet("GetUserProfile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            TokenDataDTO data = await _ts.GetTokenData(User);
            TUser user = await _us.GetUserById(Convert.ToInt32(data.UserId));
            UserProfileDTO userData = new UserProfileDTO
            {
                Username = user.FUsername,
                Email = user.FEmail,
                Phone = user.FPhone,
                IdNum = user.FIdNum,
                Address = user.FAddress,
                Image = user.FImage,
                CreateTime = user.FCreateTime.ToString("yyyy-MM-dd HH:mm:ss"),

            };

            return Ok(userData);
        }
        [HttpGet("GetUserProfile/{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile(int id)
        {

            TUser user = await _us.GetUserById(id);
            UserProfileDTO userData = new UserProfileDTO
            {
                Lastname = user.FLastName,
                Firstname = user.FFirstName,
                Username = user.FUsername,
                Email = user.FEmail,
                Phone = user.FPhone,
                IdNum = user.FIdNum,
                Address = user.FAddress,
                Image = user.FImage,
                CreateTime = user.FCreateTime.ToString("yyyy-MM-dd HH:mm:ss"),

            };

            return Ok(userData);
        }
        [HttpGet("CheckSeller")]
        [Authorize]
        public async Task<IActionResult> CheckSeller()
        {
            TokenDataDTO data = await _ts.GetTokenData(User);
            TUser user = await _us.GetUserById(Convert.ToInt32(data.UserId));
            bool exist = await _us.CheckSeller(user.FId);
            return Ok(new
            {
                isSeller = exist
            });
        }
        [Authorize]
        [HttpGet("CurrentUser")]
        public async Task<IActionResult> CurrentUser()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            TUser? user = await _us.GetUserById(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found"
                });
            }

            return Ok(new
            {
                userId = user.FId,
                userName = user.FUsername,
                userImage = user.FImage
            });
        }
        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Credential))
            {
                return BadRequest(new
                {
                    message = "缺少 Google Credential"
                });
            }

            try
            {

                var settings =
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[]
                        {
                    _config[
                        "Authentication:Google:ClientId"
                    ]!
                        }
                    };

                var payload =
                    await GoogleJsonWebSignature.ValidateAsync(
                        dto.Credential,
                        settings
                    );

                string googleUserId = payload.Subject;
                TExternalLogin? externalLogin =
                    await _us.GetExternalLogin(
                        "Google",
                        googleUserId
                    );
                if (externalLogin != null)
                {
                    TUser? user =
                        await _us.GetUserById(externalLogin.FUserId);

                    if (user == null)
                    {
                        return Unauthorized(new
                        {
                            message = "找不到綁定的會員"
                        });
                    }
                    return await GoogleLoginSuccess(user);
                    // 暫時測試
                    //return Ok(new
                    //{
                    //    message = "Google 登入成功",
                    //    userId = user.FId,
                    //    username = user.FUsername
                    //});
                }
                if (externalLogin == null)
                {
                    TUser? existingUser = await _us.GetUserByEmail(payload.Email);
                    if (existingUser != null)
                    {
                        await _us.AddExternalLogin(existingUser.FId, "Google", payload.Subject);
                        return await GoogleLoginSuccess(existingUser);
                    } else if (existingUser == null)
                    {

                    }
                }
                return Ok(new
                {
                    requiresRegistration = true,
                    googleUserId = payload.Subject,
                    email = payload.Email,
                    name = payload.Name,
                    picture = payload.Picture
                });

            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new
                {
                    message = "Google 登入驗證失敗"
                });
            }
        }
        private async Task<IActionResult> GoogleLoginSuccess(TUser user)
        {
            string token = await _ts.GenerateToken(user);

            Response.Cookies.Append(
                "token",
                token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(60),
                    Path = "/"
                }
            );

            return Ok(new
            {
                message = "Google login success"
            });
        }
        [Authorize]
        [HttpPost("EditProfile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> EditProfile(
        [FromForm] EditProfileDTO e,
        [FromForm] IFormFile? img)
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out int uid))
            {
                return Unauthorized();
            }

            try
            {
                await _us.UpdateProfile(e, uid);

                if (img != null && img.Length > 0)
                {
                    await _us.UploadImage(img, uid);
                }

                return Ok(new
                {
                    message = "個人資料更新成功"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpGet("TestEmail")]
        public async Task<IActionResult> TestEmail()
        {
            await _email.SendEmailAsync(
                "howru6948@gmail.com",
                "FriendlyFood 測試信",
                "<h2>寄信成功</h2><p>SMTP 設定正常</p>"
            );

            return Ok(new
            {
                message = "測試信已寄出"
            });
        }
        [HttpGet("VerifyEmail/{token}")]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            try
            {
                await _email.EmailVerify(token);
                return Ok(new {
                    message="Email 驗證成功",
                });

            }
            catch(Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpPost("TestIdCardOcr")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TestIdCardOcr(
        [FromForm] IFormFile idCard)
        {
            if (idCard == null || idCard.Length == 0)
            {
                return BadRequest(new
                {
                    message = "請上傳身分證圖片"
                });
            }

            var result =
            await _icps.ReadTextAsync(idCard);

            return Ok(result);
        }
        [Authorize]
        [HttpPost("Apply")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Apply([FromForm] IFormFile idcard, [FromForm] ApplyDTO dto)
        {
            if (idcard == null || idcard.Length == 0)
            {
                return BadRequest(new
                {
                    message = "請上傳身分證圖片"
                });
            }
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ocr = await _icps.ReadTextAsync(idcard);
            string formName =$"{dto.LastName}{dto.FirstName}".Replace(" ", "");

            string formId =dto.IdNumber.Trim().ToUpperInvariant();
            bool matched =
            ocr.Name == formName &&
            ocr.IdNumber == formId;
            if (!matched)
            {
                return BadRequest(new
                {
                    message = "身分資料驗證失敗"
                });
            }
            try {
                await _us.AddSeller(userId, dto.StoreName, dto.StoreDescription);
            }
            catch(Exception e)
            {
                return BadRequest(new
                {
                    message = "申請失敗",
                    error = e.Message,
                    innerError = e.InnerException?.Message
                });
            }
            return Ok(new
            {
                message = "商家建立成功"
            });
        }
        [Authorize]
        [HttpGet("GetRecipe")]
        public async Task<IActionResult> GetMyRecipes()
        {
            string? userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            UserRecipeStatDTO recipes=await _us.GetRecipe(Convert.ToInt32(userId));
            return Ok(recipes);
        
        }
        //[Authorize]
        //[HttpGet("GetPost")]
        //public async Task<IActionResult> GetPost()
        //{

        //}
        [Authorize]
        [HttpGet("GetUserRecipes/{id}")]
        public async Task<IActionResult> GetUserRecipes(int id)
        {
            var result = await _us.GetRecipe(id);

            return Ok(result);
        }
        [Authorize]
        [HttpGet("GetPost")]
        public async Task<IActionResult> GetPost()
        {
            int userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var result = await _us.GetPost(userId);

            return Ok(result);
        }
        [Authorize]
        [HttpGet("GetUserPost/{id}")]
        public async Task<IActionResult> GetUserPost(int id)
        {

            var result = await _us.GetPost(id);

            return Ok(result);
        }
    }
}
