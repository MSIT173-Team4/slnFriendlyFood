using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Extensions;
using prjFriendlyFoodWebAPI.Models;

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

builder.Services.AddControllers();
builder.Services.AddOpenApi();
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

app.UseHttpsRedirection();
app.UseCors("AllowAngularClient");
app.UseAuthorization();
app.MapControllers();

app.Run();
