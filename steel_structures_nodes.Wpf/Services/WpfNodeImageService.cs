using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Steel_structures_nodes_public_project.Domain.Repositories;

namespace Steel_structures_nodes_public_project.Wpf.Services;

/// <summary>
/// Загружает изображения узлов из MongoDB (коллекция NodeNotesImagesDB).
/// Аналог NodeImageService из MAUI, но возвращает WPF-совместимый ImageSource.
/// </summary>
public sealed class WpfNodeImageService
{
    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];
    private const int MaxNumberedVariants = 20;

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
            var filenames = BuildAllPossibleFilenames(nodeCode);

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

    // ─── Вспомогательные методы (аналогичны MAUI NodeImageService) ──────────

    private static List<string> BuildAllPossibleFilenames(string nodeCode)
    {
        var candidates = BuildCodeCandidates(nodeCode);
        var list = new List<string>();

        foreach (var c in candidates)
        {
            foreach (var ext in Extensions)
                list.Add(c + ext);

            for (int i = 1; i <= MaxNumberedVariants; i++)
                foreach (var ext in Extensions)
                    list.Add($"{c}_{i}{ext}");
        }

        return list;
    }

    private static string[] BuildCodeCandidates(string nodeCode)
    {
        var c0 = (nodeCode ?? string.Empty).Trim();
        if (c0.Length == 0)
            return [];

        var list = new List<string>();

        void Add(string s)
        {
            s = s.Trim();
            if (s.Length > 0 && !list.Contains(s, StringComparer.OrdinalIgnoreCase))
                list.Add(s);
        }

        Add(c0);
        Add(ExtractPrefix(c0));

        var dash = c0.IndexOf('-');
        if (dash > 0) Add(c0[..dash]);

        foreach (var s in list.ToArray())
        {
            if (s.EndsWith('_'))
                Add(s.TrimEnd('_'));
            else
                Add(s + "_");
        }

        return [.. list];
    }

    private static string ExtractPrefix(string code)
    {
        var t = code.Trim();
        var i = t.IndexOf('_');
        return i > 0 ? t[..i] : t;
    }
}
