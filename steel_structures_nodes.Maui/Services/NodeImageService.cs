using Steel_structures_nodes_public_project.Domain.Repositories;

namespace Steel_structures_nodes_public_project.Maui.Services;

/// <summary>
/// Загружает изображения узлов из MongoDB (коллекция NodeNotesImagesDB).
/// </summary>
public sealed class NodeImageService
{
    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];
    private const int MaxNumberedVariants = 20;

    private readonly INodeImageRepository _repository;

    public NodeImageService(INodeImageRepository repository)
    {
        _repository = repository;
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
            // Строим полный список точных имён файлов для запроса
            var filenames = BuildAllPossibleFilenames(nodeCode);

            // Индекс для сортировки по порядку из сгенерированного списка
            var order = filenames
                .Select((f, i) => (f.ToLowerInvariant(), i))
                .ToDictionary(x => x.Item1, x => x.i);

            var images = await _repository.GetByFilenamesAsync(filenames, cancellationToken);

            // Сортируем результат по порядку из filenames
            var sorted = images.OrderBy(img =>
                order.TryGetValue(img.Filename.ToLowerInvariant(), out var idx) ? idx : int.MaxValue);

            foreach (var (_, data) in sorted)
            {
                if (data.Length == 0) continue;
                var bytes = data; // копия для замыкания
                result.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NodeImageService error: {ex.Message}");
        }

        return result;
    }

    // ─── Вспомогательные методы ──────────────────────────────────────────────

    /// <summary>
    /// Генерирует все возможные точные имена файлов для заданного кода узла.
    /// Пример для "BH_1": BH_1.png, BH.png, BH_1.jpg, ..., BH_1_1.png, BH_1_2.png, ...
    /// </summary>
    private static List<string> BuildAllPossibleFilenames(string nodeCode)
    {
        var candidates = BuildCodeCandidates(nodeCode);
        var list = new List<string>();

        foreach (var c in candidates)
        {
            // Без суффикса: BH.png, BH.jpg, …
            foreach (var ext in Extensions)
                list.Add(c + ext);

            // С числовым суффиксом: BH_1.png, BH_2.png, …
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
