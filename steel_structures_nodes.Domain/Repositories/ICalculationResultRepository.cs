using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Domain.Repositories;

/// <summary>
/// Репозиторий для сохранения и получения результатов расчёта (коллекция Result).
/// </summary>
public interface ICalculationResultRepository
{
    Task AddAsync(CalculationResult result, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalculationResult>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CalculationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
