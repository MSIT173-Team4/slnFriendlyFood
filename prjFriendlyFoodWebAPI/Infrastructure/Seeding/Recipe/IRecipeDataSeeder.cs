namespace prjFriendlyFoodWebAPI.Infrastructure.Seeding.Recipe;

public interface IRecipeDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
