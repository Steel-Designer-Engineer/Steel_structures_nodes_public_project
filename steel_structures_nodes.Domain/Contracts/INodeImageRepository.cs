namespace steel_structures_nodes.Domain.Contracts;

/// <summary>
/// Репозиторий изображений узлов (коллекция NodeNotesImagesDB).
/// </summary>
public interface INodeImageRepository
{
    /// <summary>
    /// Возвращает список (имя файла, байты) для заданного набора точных имён файлов.
    /// </summary>
    Task<IReadOnlyList<(string Filename, byte[] Data)>> GetByFilenamesAsync(
        IEnumerable<string> filenames,
        CancellationToken cancellationToken = default);
}
