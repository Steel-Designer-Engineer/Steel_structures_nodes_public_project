using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Domain.Contracts;

/// <summary>
/// Узкий интерфейс чтения документов таблиц взаимодействия.
/// </summary>
public interface IInteractionTableReadRepository
{
    Task<InteractionTable?> GetByNameAndConnectionCodeAsync(string name, string connectionCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InteractionTable>> GetAllAsync(CancellationToken cancellationToken = default);
}
