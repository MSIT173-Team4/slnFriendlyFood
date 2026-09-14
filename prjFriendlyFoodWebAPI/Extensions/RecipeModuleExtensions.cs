using prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;
using prjFriendlyFoodWebAPI.ExternalServices.SmartBot;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Extensions;

public static class RecipeModuleExtensions
{
    public static IServiceCollection AddRecipeModule(this IServiceCollection services)
    {
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IRecipePantryService, RecipePantryService>();
        services.AddScoped<IRecipeEngagementService, RecipeEngagementService>();
        services.AddScoped<IRecipeRecommendationService, RecipeRecommendationService>();
        services.AddScoped<IRecipeIngredientNormalizationService, RecipeIngredientNormalizationService>();
        services.AddScoped<IRecipeAssetService, RecipeAssetService>();
        services.AddScoped<IIngredientNameNormalizer, IngredientNameNormalizer>();
        services.AddScoped<IPantryIntakeService, PantryIntakeService>();
        services.AddScoped<IRecipeDataSeeder, RecipeDevelopmentDataSeeder>();
        services.AddHttpClient<IPantryAiClient, SmartBotPantryAiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["SmartBot:BaseUrl"] ?? "https://localhost:7189/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(70);
        });
        services.AddHttpClient<IRecipeAiClient, RecipeAiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["SmartBot:BaseUrl"] ?? "https://localhost:7189/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(70);
        });

        return services;
    }

    public static async Task SeedRecipeDevelopmentDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IRecipeDataSeeder>();
        await seeder.SeedAsync();
    }
}
