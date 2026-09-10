using prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;
using prjFriendlyFoodWebAPI.Services.Recipe;

namespace prjFriendlyFoodWebAPI.Extensions;

public static class RecipeModuleExtensions
{
    public static IServiceCollection AddRecipeModule(this IServiceCollection services)
    {
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IRecipePantryService, RecipePantryService>();
        services.AddScoped<IRecipeEngagementService, RecipeEngagementService>();
        services.AddScoped<IRecipeDataSeeder, RecipeDevelopmentDataSeeder>();

        return services;
    }

    public static async Task SeedRecipeDevelopmentDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IRecipeDataSeeder>();
        await seeder.SeedAsync();
    }
}
