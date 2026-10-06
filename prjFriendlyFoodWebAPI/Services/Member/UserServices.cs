using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.DTOs.Member;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.Recipe;
using System.Security.Claims;
using System.Security.Cryptography;

namespace prjFriendlyFoodWebAPI.Services.Member
{
    public class UserServices
    {
        private readonly FriendlyFoodDbContext _db;
        private readonly IWebHostEnvironment _env;
        public UserServices(FriendlyFoodDbContext db,IWebHostEnvironment env)
        {
            _env = env;
            _db = db;
        }
        public async Task<bool> IsUsernameExists(string username)
        {
            return await Task.Run(() => _db.TUsers.Any(user => user.FUsername == username));
        }
        public async Task<bool> IsEmailExists(string email)
        {
            return await Task.Run(() => _db.TUsers.Any(user => user.FEmail == email));
        }
        //register user
        public async Task<TUser> AddUser(UserRegisterDTO u, string password)
        {
            TUser user = new TUser
            {
                FUsername = u.fUsername,
                FPassword = password,
                FEmail = u.fEmail,
                FPhone = u.fPhone,
                FAddress = u.fAddress,
                FIdNum = u.fIdNum,
                FCreateTime = DateTime.Now,
                FIsAdmin = false
            };

            _db.TUsers.Add(user);

            await _db.SaveChangesAsync();
            user = await _db.TUsers.FirstOrDefaultAsync(x => x.FUsername == u.fUsername);
            return user;
        }

        //edit user profile
        public async Task<TUser> EditProfile(UserEditDTO u,int id)
        {
            TUser user = await _db.TUsers.FirstOrDefaultAsync(x => x.FId == id);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }
            user.FUsername = u.fUsername;
            user.FEmail = u.fEmail;
            user.FPhone = u.fPhone;
            user.FImage = u.fImage;
            user.FAddress = u.fAddress;
            user.FIdNum = u.fIdNum;
            await _db.SaveChangesAsync();
            return user;
        }
        public async Task<TUser> GetUserById(int id)
        {
            var user = await Task.Run(() => _db.TUsers.FirstOrDefault(u => u.FId == id));
            return user;
        }
        public async Task<string> GetPasswordByUsername(string username)
        {
            var user = await Task.Run(() => _db.TUsers.FirstOrDefault(u => u.FUsername == username));
            return user?.FPassword;
        }
        public async Task<string> GetPasswordByEmail(string email)
        {
            var user = await Task.Run(() => _db.TUsers.FirstOrDefault(u => u.FEmail == email));
            return user?.FPassword;
        }
        public async Task<TUser> GetUserByUsername(string username)
        {
            var user = await Task.Run(() => _db.TUsers.FirstOrDefault(u => u.FUsername == username));
            return user;
        }
        public async Task<TUser> GetUserByEmail(string email)
        {
            var user = await Task.Run(() => _db.TUsers.FirstOrDefault(u => u.FEmail == email));
            return user;
        }
        public async Task<TExternalLogin?> GetExternalLogin(
        string provider,
        string providerUserId)
        {
            return await _db.TExternalLogins.FirstOrDefaultAsync(
                x => x.FProvider == provider && x.FProviderUserId == providerUserId);
        }
        public async Task<TExternalLogin> AddExternalLogin(
        int userId,
        string provider,
        string providerUserId)
        {
            TExternalLogin externalLogin = new TExternalLogin
            {
                FUserId = userId,
                FProvider = provider,
                FProviderUserId = providerUserId
            };

            _db.TExternalLogins.Add(externalLogin);

            await _db.SaveChangesAsync();

            return externalLogin;
        }
        public async Task UploadImage(IFormFile img,int uid)
        {
            TUser user = _db.TUsers.FirstOrDefault(x => x.FId == uid);
            if (user == null)
            {
                throw new Exception("找不到會員");
            }

            if (img == null || img.Length == 0)
            {
                throw new ArgumentException("圖片不能為空");
            }
            string fileName = $"{Guid.NewGuid()}.jpg";
            string folderPath = Path.Combine(_env.WebRootPath,"images","Member");
            string filePath = Path.Combine(
                folderPath,
                fileName
            );
            using (FileStream stream = new FileStream(filePath,FileMode.Create))
            {
                await img.CopyToAsync(stream);
            }
            string imageUrl = $"/images/Member/{fileName}";

            user.FImage = imageUrl;

            await _db.SaveChangesAsync();

            
        }
        public async Task UpdateProfile(EditProfileDTO dto,int id) 
        {
            TUser? user = await _db.TUsers
            .FirstOrDefaultAsync(x => x.FId == id);
            if (user == null)
            {
                throw new Exception("找不到會員");
            }
            user.FLastName = dto.LastName;
            user.FFirstName = dto.FirstName;
            user.FUsername = dto.Username;
            user.FPhone = dto.Phone;
            user.FAddress = dto.Address;
            await _db.SaveChangesAsync();
            
        }
        public async Task<bool> CheckSeller(int id)
        {
            bool exist=await _db.TSellers.AnyAsync(e => e.FUserId == id);
            return exist;
        }
        public async Task AddSeller(int id,string name,string? desc)
        {
            bool exists = await _db.TSellers.AnyAsync(x => x.FUserId == id);

            if (exists)
            {
                throw new ArgumentException("此會員已具有商家資格");
            }
            TSeller s = new TSeller();
            s.FUserId = id;
            s.FSellerName = name;
            s.FDescription = desc;
            s.FStatus = 4;
            s.FApplyDate = DateTime.UtcNow;
            _db.TSellers.Add(s);
            await _db.SaveChangesAsync();
        
        }
        public async Task<UserRecipeStatDTO> GetRecipe(int id)
        {
            var recipes = await _db.TRecipes
            .Where(r => r.FUserId == id && r.FStatus == 1)
            .Select(r => new UserRecipeDTO
            {
                RecipeId = r.FRecipeId,
                Title = r.FTitle,
                CoverImageUrl = r.FCoverImageUrl,
                Views = r.FViews,
                Likes = r.FLikes,
                Favorites = r.FFavorites,
                CreatedAt = r.FCreatedAt
            })
            .ToListAsync();
            foreach (var recipe in recipes)
            {
                recipe.CoverImageUrl = RecipeImageUrlResolver.Resolve(recipe.Title, recipe.CoverImageUrl);
            }

            var result = new UserRecipeStatDTO
            {
                Recipes = recipes,
                TotalViews = recipes.Sum(r => r.Views),
                TotalLike=recipes.Sum(r => r.Likes),
            };
            return result;
        }
        public async Task<UserPostStatDTO> GetPost(int id)
        {
            var posts = await _db.TPostTables
                .Where(p => p.FUserId == id)
                .Select(p => new UserPostDTO
                {
                    PostId = p.FPostId,
                    Title = p.FTitle,
                    Likes = p.FLikes,
                    Views = p.FViews,
                    PostDate = p.FPostDate
                })
                .ToListAsync();

            return new UserPostStatDTO
            {
                Posts = posts,
                TotalLikes = posts.Sum(p => p.Likes),
                TotalViews = posts.Sum(p => p.Views)
            };
        }
    }
}

