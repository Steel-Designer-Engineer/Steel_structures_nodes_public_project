using Steel_structures_nodes_public_project.Domain.Entities;

namespace Steel_structures_nodes_public_project.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с профилями
/// </summary>
public interface IProfileRepository
{
    Task<Profile?> GetByNameAsync(string profileName, CancellationToken cancellationToken = default);
    Task<Profile?> GetByGuidAsync(Guid connectionGuid, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Profile profile, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Profile> profiles, CancellationToken cancellationToken = default);
    Task UpdateAsync(Profile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
