using StackExchange.Redis;
namespace prjFriendlyFoodWebAPI.Services.Member
{
    public class RedisServices
    {
        private readonly IDatabase _db;

        public RedisServices(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }
        public async Task SetAsync(
            string key,
            string value,
            TimeSpan expiry )
        {
            await _db.StringSetAsync(
                key,
                value,
                expiry
            );
        }

        public async Task<string?> GetAsync(string key)
        {
            var value = await _db.StringGetAsync(key);

            return value.HasValue ? value.ToString() : null;
        }

        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
}
