using Steel_structures_nodes_public_project.Domain.Entities;

namespace Steel_structures_nodes_public_project.Domain.Repositories;

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
    Task AddAsync(InteractionTable interactionTable, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<InteractionTable> interactionTables, CancellationToken cancellationToken = default);
    Task UpdateAsync(InteractionTable interactionTable, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
