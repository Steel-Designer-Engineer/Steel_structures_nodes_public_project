using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с профилями
/// </summary>
public interface IProfileRepository
{
    Task<Profile?> GetByNameAsync(string profileName, CancellationToken cancellationToken = default);
    Task<Profile?> GetByGuidAsync(Guid connectionGuid, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken cancellationToken = default);
}
