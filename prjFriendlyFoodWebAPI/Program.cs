using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Interfaces;
using prjFriendlyFoodWebAPI.ExternalServices.FoodMap.Google.Models;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;
using prjFriendlyFoodWebAPI.Services.Member;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
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
