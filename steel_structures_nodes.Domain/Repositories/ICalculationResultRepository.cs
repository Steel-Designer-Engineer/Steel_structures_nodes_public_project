using Steel_structures_nodes_public_project.Domain.Entities;

namespace Steel_structures_nodes_public_project.Domain.Repositories;

/// <summary>
/// Репозиторий для сохранения и получения результатов расчёта (коллекция Result).
/// </summary>
public interface ICalculationResultRepository
{
    Task AddAsync(CalculationResult result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalculationResult>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CalculationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
