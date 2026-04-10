using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Maui.Services;

/// <summary>
/// Fallback-репозиторий, когда MongoDB недоступна.
/// Возвращает пустые данные — приложение запускается, но показывает ошибку.
/// </summary>
internal sealed class OfflineInteractionTableRepository : IInteractionTableRepository
{
    private readonly string _errorMessage;

    public OfflineInteractionTableRepository(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public string ErrorMessage => _errorMessage;

    public Task<IReadOnlyList<string>> GetDistinctNamesAsync(CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameAsync(string name, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameAndBeamAsync(string name, string beamProfile, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameAndColumnAsync(string name, string columnProfile, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameColumnAndBeamAsync(string name, string columnProfile, string beamProfile, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<InteractionTable?> GetByNameAndConnectionCodeAsync(string name, string connectionCode, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetDistinctProfileBeamsByNameAsync(string name, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetDistinctProfileBeamsByNameAndColumnAsync(string name, string columnProfile, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<string>> GetDistinctProfileColumnsByNameAsync(string name, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task<IReadOnlyList<InteractionTable>> GetAllAsync(CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task AddAsync(InteractionTable interactionTable, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task AddRangeAsync(IEnumerable<InteractionTable> interactionTables, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task UpdateAsync(InteractionTable interactionTable, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new InvalidOperationException($"БД недоступна: {_errorMessage}");
}
