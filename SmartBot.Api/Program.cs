using SmartBot.Api.Configurations;
using SmartBot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddOptions<GeminiAgentOptions>()
    .Bind(builder.Configuration.GetSection(GeminiAgentOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        $"{GeminiAgentOptions.SectionName}:ApiKey 不可為空。")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ModelName),
        $"{GeminiAgentOptions.SectionName}:ModelName 不可為空。")
    .ValidateOnStart();
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
