using Microsoft.EntityFrameworkCore;
using prjFriendlyFoodWebAPI.Models;

var builder = WebApplication.CreateBuilder(args);
//CORS 跨來源資源共用
builder.Services.AddCors(options =>
{
options.AddPolicy("AllowAngularClient", policy =>
{
policy.WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod();
});
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<FriendlyFoodDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
//CORS中介 放置在 UseHttpsRedirection 之後、UseAuthorization 之前
app.UseCors("AllowAngularClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
