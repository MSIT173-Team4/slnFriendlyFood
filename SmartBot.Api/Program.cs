using DotNetEnv; 
using SmartBot.Api.Services;

var builder = WebApplication.CreateBuilder(args);
//載入.env的GEMINI_API_KEY
Env.TraversePath().Load();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddOpenApi();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
