using Microsoft.IdentityModel.Tokens;
using prjFriendlyFoodWebAPI.DTOs.Member;
using prjFriendlyFoodWebAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace prjFriendlyFoodWebAPI.Services.Member
{
    public class TokenServices
    {
        private readonly IConfiguration _config;
        private readonly EncodeServices _es;
        private readonly FriendlyFoodDbContext _db;
        public TokenServices(IConfiguration configuration, EncodeServices es,FriendlyFoodDbContext db)
        {
            _config = configuration;
            _db = db;
            _es = es;
        }
        //jwt token(access token)
        public async Task<string> GenerateToken(TUser u)
        {

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub,u.FId.ToString()),
                new Claim(ClaimTypes.Name,u.FUsername),
                new Claim(ClaimTypes.Role,u.FIsAdmin?"Admin":"User")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        //other token(refresh token,email verify mail token and password change mail token)
        public async Task<string> GernateTokenString(int id,string usedAt)
        {
            TEmailVerification tokenData = new TEmailVerification();
            
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToHexString(bytes);
            tokenData.FUserId = id;
            tokenData.FToken = token;
            tokenData.FType = usedAt;
            tokenData.FExpireAt = DateTime.UtcNow.AddMinutes(30);
            _db.TEmailVerifications.Add(tokenData);
            await _db.SaveChangesAsync();
            return token;
        }
        public async Task<TokenDataDTO> GetTokenData(ClaimsPrincipal user)
        {
            return new TokenDataDTO
            {
                UserId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Roles = user.FindFirst(ClaimTypes.Role)?.Value
            };
        }
    }
}
