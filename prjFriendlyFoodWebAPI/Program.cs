using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Models;
using prjFriendlyFoodWebAPI.Services.FoodMap;
using prjFriendlyFoodWebAPI.Services.FoodMap.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartHeadersLengthLimit = 10 * 1024 * 1024;
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024;
    options.ValueLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IFoodMapService, PlaceService>();
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
