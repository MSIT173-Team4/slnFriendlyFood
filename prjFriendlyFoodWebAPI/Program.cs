using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;
using prjFriendlyFoodWebAPI.Services.ImageUpload;
using prjFriendlyFoodWebAPI.Services.Market;
using prjFriendlyFoodWebAPI.Services.Member;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Cloudinary-Star
//=================================
var cloudName = builder.Configuration["Cloudinary:CloudName"];
var apiKey = builder.Configuration["Cloudinary:ApiKey"];
var apiSecret = builder.Configuration["Cloudinary:ApiSecret"];

if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
    throw new InvalidOperationException("Cloudinary 設定未填寫，請檢查 appsettings.Development.json");

var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
cloudinary.Api.Secure = true;   // 回傳 https 網址

builder.Services.AddSingleton(cloudinary);
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
//================================
//Cloudinary-End

builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ISellerIdentityService, SellerIdentityService>();
// CORS 允許的前端網址從設定讀取（appsettings 的 Cors:AllowedOrigins，
// 或環境變數 Cors__AllowedOrigins__0、Cors__AllowedOrigins__1 ...）；沒設定時用本機開發的預設值。
// 正式環境前端透過 nginx 轉發 /api，前後端同網域，不會觸發 CORS。
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:4200", "http://127.0.0.1:4200"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddScoped<EmailServices>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("token"))
            {
                context.Token = context.Request.Cookies["token"];
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartHeadersLengthLimit = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
    options.ValueLengthLimit = 10 * 1024 * 1024;
});
builder.Services.Configure<GooglePlacesOptions>(
    builder.Configuration
        .GetSection("GoogleMaps"));

builder.Services.AddHttpClient<
    IGooglePlacesClient,
    GooglePlacesClient>(
    (serviceProvider, client) =>
    {
        var options =
            serviceProvider
                .GetRequiredService<
                    IOptions<GooglePlacesOptions>>()
                .Value;

        client.BaseAddress =
            new Uri(options.PlacesBaseUrl);

        client.Timeout =
            TimeSpan.FromSeconds(10);
    });

builder.Services.AddScoped<IdCardProofingServices>();
builder.Services.AddHttpClient<IGoogleRoutesClient, GoogleRoutesClient>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<EncodeServices>();
builder.Services.AddScoped<UserServices>();
builder.Services.AddScoped<TokenServices>();
builder.Services.AddScoped<IShoppingListMappingService, ShoppingListMappingService>();
builder.Services.AddScoped<IFoodMapService, PlaceService>();
builder.Services.AddScoped<IRecommendationServices, RecommendationService>();
builder.Services.AddScoped<ITripServices, TripServices>();
builder.Services.AddScoped<ITripOptimizationService, TripOptimizationService>();
builder.Services.AddScoped<ITripPlanningService, TripPlanningService>();
builder.Services.AddDbContext<FriendlyFoodDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddRecipeModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    if (app.Configuration.GetValue<bool>("DataSeeding:SeedRecipeDemoData"))
    {
        await app.SeedRecipeDevelopmentDataAsync();
    }
}

app.UseCors("AllowAngularClient");
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
