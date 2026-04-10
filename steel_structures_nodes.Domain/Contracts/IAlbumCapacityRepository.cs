using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Domain.Contracts;

/// <summary>
/// Репозиторий для работы с несущей способностью узлов из альбома
/// </summary>
public interface IAlbumCapacityRepository
{
    Task<AlbumCapacity?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlbumCapacity>> GetAllAsync(CancellationToken cancellationToken = default);
}
