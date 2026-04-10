using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Domain.Services.NodeImages;

namespace steel_structures_nodes.Wpf.Services;

/// <summary>
/// Загружает изображения узлов из MongoDB (коллекция NodeNotesImagesDB).
/// Аналог NodeImageService из MAUI, но возвращает WPF-совместимый ImageSource.
/// </summary>
public sealed class WpfNodeImageService : IWpfNodeImageService
{
    private readonly INodeImageRepository _repository;

    public WpfNodeImageService(INodeImageRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Асинхронно возвращает список <see cref="ImageSource"/> для заданного кода узла.
    /// Должен вызываться из UI-потока — BitmapImage создаётся на нём же после await.
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
            var filenames = NodeImageFilenameBuilder.BuildAllPossibleFilenames(nodeCode);

            // Индекс для сортировки по порядку из сгенерированного списка
            var order = filenames
                .Select((f, i) => (f.ToLowerInvariant(), i))
                .ToDictionary(x => x.Item1, x => x.Item2);

            var images = await _repository.GetByFilenamesAsync(filenames, cancellationToken);

            // Сортируем по исходному порядку filenames
            var sorted = images.OrderBy(img =>
                order.TryGetValue(img.Filename.ToLowerInvariant(), out var idx) ? idx : int.MaxValue);

            // BitmapImage создаётся в продолжении await — на UI-потоке
            foreach (var (_, data) in sorted)
            {
                if (data.Length == 0) continue;
                try
                {
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = new MemoryStream(data);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bi.EndInit();
                    bi.Freeze();
                    result.Add(bi);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BitmapImage error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WpfNodeImageService error: {ex.Message}");
        }

        return result;
    }
}
