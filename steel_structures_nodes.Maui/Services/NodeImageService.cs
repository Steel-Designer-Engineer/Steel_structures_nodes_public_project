using System.Collections.Concurrent;
using steel_structures_nodes.Domain.Services.NodeImages;
using steel_structures_nodes.Domain.Contracts;

namespace steel_structures_nodes.Maui.Services;

/// <summary>
/// Загружает изображения узлов.
/// Двухуровневый кэш: in-memory → Redis → MongoDB.
/// </summary>
public sealed class NodeImageService : INodeImageService
{
    private readonly INodeImageRepository _repository;
    private readonly RedisImageCacheService? _redisCache;

    // L1: in-memory кэш (живёт пока работает приложение)
    private readonly ConcurrentDictionary<string, List<(string Filename, byte[] Data)>> _memCache = new();

    public NodeImageService(INodeImageRepository repository, RedisImageCacheService? redisCache = null)
    {
        _repository = repository;
        _redisCache = redisCache;
    }

    /// <summary>
    /// Асинхронно возвращает список <see cref="ImageSource"/> для заданного кода узла.
    /// </summary>
    public async Task<List<ImageSource>> LoadAllNodeImagesAsync(
        string nodeCode,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ImageSource>();
        if (string.IsNullOrWhiteSpace(nodeCode))
            return result;

        try
        {
            var cacheKey = nodeCode.Trim().ToLowerInvariant();

            if (!_memCache.TryGetValue(cacheKey, out var cachedImages))
            {
                // L2: пробуем Redis
                if (_redisCache is { IsAvailable: true })
                {
                    var fromRedis = await _redisCache.TryGetAsync(nodeCode);
                    if (fromRedis is { Count: > 0 })
                    {
                        // Восстанавливаем порядок по сгенерированным именам
                        var filenames = NodeImageFilenameBuilder.BuildAllPossibleFilenames(nodeCode);
                        var order = filenames
                            .Select((f, i) => (f.ToLowerInvariant(), i))
                            .ToDictionary(x => x.Item1, x => x.i);

                        cachedImages = fromRedis
                            .OrderBy(img =>
                                order.TryGetValue(img.Filename.ToLowerInvariant(), out var idx) ? idx : int.MaxValue)
                            .ToList();

                        _memCache[cacheKey] = cachedImages;
                        System.Diagnostics.Debug.WriteLine($"NodeImageService: {cacheKey} — из Redis ({cachedImages.Count} шт.)");
                    }
                }

                // L3: MongoDB
                if (cachedImages is null)
                {
                    var filenames = NodeImageFilenameBuilder.BuildAllPossibleFilenames(nodeCode);

                    var order = filenames
                        .Select((f, i) => (f.ToLowerInvariant(), i))
                        .ToDictionary(x => x.Item1, x => x.i);

                    var images = await _repository.GetByFilenamesAsync(filenames, cancellationToken);

                    cachedImages = images
                        .Where(img => img.Data.Length > 0)
                        .OrderBy(img =>
                            order.TryGetValue(img.Filename.ToLowerInvariant(), out var idx) ? idx : int.MaxValue)
                        .Select(img => (img.Filename, img.Data))
                        .ToList();

                    _memCache[cacheKey] = cachedImages;

                    // Записываем в Redis в фоне
                    if (_redisCache is { IsAvailable: true } && cachedImages.Count > 0)
                    {
                        _ = Task.Run(async () =>
                        {
                            try { await _redisCache.SetAsync(nodeCode, cachedImages); }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Redis write error: {ex.Message}");
                            }
                        });
                    }

                    System.Diagnostics.Debug.WriteLine($"NodeImageService: {cacheKey} — из MongoDB ({cachedImages.Count} шт.)");
                }
            }

            foreach (var (_, data) in cachedImages)
            {
                var bytes = data;
                result.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NodeImageService error: {ex.Message}");
        }

        return result;
    }
}
