using System.Collections.Concurrent;

namespace prjFriendlyFoodWebAPI.Services.FoodMap
{
    // FoodMap 專用的行程內快取。
    // 刻意不用 IMemoryCache：那需要在 Program.cs 呼叫 AddMemoryCache()，
    // 這次只改 FoodMap 範圍，所以用一個靜態的簡易快取代替。
    // 伺服器重啟就會清空，多台伺服器之間也不共用（單機開發/部署夠用）。
    internal static class FoodMapMemoryCache
    {
        private const int MaxEntries = 2000;

        private static readonly ConcurrentDictionary<string, CacheEntry> Entries = new();

        private sealed record CacheEntry(DateTime ExpiresAtUtc, object Value);

        public static bool TryGet<T>(string key, out T value)
        {
            if (Entries.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAtUtc > DateTime.UtcNow && entry.Value is T typed)
                {
                    value = typed;
                    return true;
                }

                Entries.TryRemove(key, out _);
            }

            value = default!;
            return false;
        }

        public static void Set<T>(string key, T value, TimeSpan timeToLive) where T : notnull
        {
            if (Entries.Count >= MaxEntries)
            {
                RemoveExpired();

                if (Entries.Count >= MaxEntries)
                {
                    Entries.Clear();
                }
            }

            Entries[key] = new CacheEntry(DateTime.UtcNow.Add(timeToLive), value);
        }

        private static void RemoveExpired()
        {
            var now = DateTime.UtcNow;

            foreach (var pair in Entries)
            {
                if (pair.Value.ExpiresAtUtc <= now)
                {
                    Entries.TryRemove(pair.Key, out _);
                }
            }
        }
    }
}
