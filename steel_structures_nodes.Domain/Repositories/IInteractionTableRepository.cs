using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Domain.Repositories;

/// <summary>
/// Репозиторий для работы с таблицами взаимодействия
/// </summary>
public interface IInteractionTableRepository
{
    Task<IReadOnlyList<string>> GetDistinctNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetConnectionCodesByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetConnectionCodesByNameAndBeamAsync(string name, string beamProfile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetConnectionCodesByNameAndColumnAsync(string name, string columnProfile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetConnectionCodesByNameColumnAndBeamAsync(string name, string columnProfile, string beamProfile, CancellationToken cancellationToken = default);
    Task<InteractionTable?> GetByNameAndConnectionCodeAsync(string name, string connectionCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctProfileBeamsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctProfileBeamsByNameAndColumnAsync(string name, string columnProfile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDistinctProfileColumnsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InteractionTable>> GetAllAsync(CancellationToken cancellationToken = default);
}
