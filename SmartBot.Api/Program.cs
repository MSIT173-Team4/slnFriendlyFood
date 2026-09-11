using DotNetEnv;
using SmartBot.Api.Services;

var builder = WebApplication.CreateBuilder(args);
Env.TraversePath().Load();

builder.Services.AddControllers();
builder.Services.AddHttpClient<IGeminiService, GeminiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddOpenApi();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
