using Steel_structures_nodes_public_project.Domain.Entities;

namespace Steel_structures_nodes_public_project.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с несущей способностью узлов из альбома
/// </summary>
public interface IAlbumCapacityRepository
{
    Task<AlbumCapacity?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlbumCapacity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AlbumCapacity albumCapacity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<AlbumCapacity> albumCapacities, CancellationToken cancellationToken = default);
    Task UpdateAsync(AlbumCapacity albumCapacity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
