using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
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
        public UsersController(UserServices userServices, EncodeServices encodeServices, TokenServices tokenServices)
        {
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
            return Ok(new
            {
                message = "User registered successfully"
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
                Expires = DateTime.UtcNow.AddMinutes(15),
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
            UserProfileDTO userData= new UserProfileDTO
            {
                Username = user.FUsername,
                Email = user.FEmail,
                Phone = user.FPhone,
                IdNum = user.FIdNum,
                Address = user.FAddress,
                Image = user.FImage,
                CreateTime = user.FCreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                LastLogin = user.FLastLogin?.ToString("yyyy-MM-dd HH:mm:ss")
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
                Username = user.FUsername,
                Email = user.FEmail,
                Phone = user.FPhone,
                IdNum = user.FIdNum,
                Address = user.FAddress,
                Image = user.FImage,
                CreateTime = user.FCreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                LastLogin = user.FLastLogin?.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return Ok(userData);
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
                userName = user.FUsername,
                userImage = user.FImage
            });
        }
    }
}
