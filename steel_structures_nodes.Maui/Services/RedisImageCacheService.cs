using StackExchange.Redis;

namespace steel_structures_nodes.Maui.Services;

/// <summary>
/// Кэш изображений узлов в Redis.
/// Ключ: "img:{nodeCode}" ? Hash { filename ? imageData }.
/// TTL: 24 часа (автоматическое обновление при обращении).
/// </summary>
public sealed class RedisImageCacheService : IDisposable
{
    private const string KeyPrefix = "img:";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;

    public bool IsAvailable => _redis is { IsConnected: true };

    public RedisImageCacheService(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.ConnectTimeout = 5_000;
            options.SyncTimeout = 3_000;
            options.AsyncTimeout = 5_000;
            options.AbortOnConnectFail = false;

            _redis = ConnectionMultiplexer.Connect(options);
            _db = _redis.GetDatabase();

            System.Diagnostics.Debug.WriteLine("RedisImageCacheService: connected");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RedisImageCacheService: connection failed — {ex.Message}");
            _redis = null;
            _db = null;
        }
    }

    /// <summary>
    /// Пытается достать все изображения для <paramref name="nodeCode"/> из Redis.
    /// Возвращает <c>null</c> если ключ не найден или Redis недоступен.
    /// </summary>
    public async Task<List<(string Filename, byte[] Data)>?> TryGetAsync(string nodeCode)
    {
        if (_db is null) return null;

        try
        {
            var key = KeyPrefix + nodeCode.Trim().ToLowerInvariant();
            var entries = await _db.HashGetAllAsync(key);

            if (entries.Length == 0)
                return null;

            var result = new List<(string, byte[])>(entries.Length);
            foreach (var entry in entries)
            {
                var filename = entry.Name.ToString();
                var data = (byte[])entry.Value!;
                if (data.Length > 0)
                    result.Add((filename, data));
            }

            // Продлеваем TTL при чтении
            _ = _db.KeyExpireAsync(key, DefaultTtl, flags: CommandFlags.FireAndForget);

            return result.Count > 0 ? result : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RedisImageCache.TryGet error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Сохраняет изображения для <paramref name="nodeCode"/> в Redis.
    /// </summary>
    public async Task SetAsync(string nodeCode, List<(string Filename, byte[] Data)> images)
    {
        if (_db is null || images.Count == 0) return;

        try
        {
            var key = KeyPrefix + nodeCode.Trim().ToLowerInvariant();

            var entries = images
                .Select(img => new HashEntry(img.Filename, img.Data))
                .ToArray();

            await _db.HashSetAsync(key, entries);
            await _db.KeyExpireAsync(key, DefaultTtl);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RedisImageCache.Set error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}
