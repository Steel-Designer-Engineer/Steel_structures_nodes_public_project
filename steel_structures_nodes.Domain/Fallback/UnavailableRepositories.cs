using steel_structures_nodes.Domain.Contracts;
using steel_structures_nodes.Domain.Entities;

namespace steel_structures_nodes.Domain.Fallback;

public sealed class UnavailableInteractionTableRepository : IInteractionTableRepository
{
    private readonly string _message;

    public UnavailableInteractionTableRepository(string message)
    {
        _message = message;
    }

    public Task<IReadOnlyList<string>> GetDistinctNamesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameAndBeamAsync(string name, string beamProfile, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameAndColumnAsync(string name, string columnProfile, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetConnectionCodesByNameColumnAndBeamAsync(string name, string columnProfile, string beamProfile, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetDistinctProfileBeamsByNameAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetDistinctProfileBeamsByNameAndColumnAsync(string name, string columnProfile, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetDistinctProfileColumnsByNameAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<InteractionTable?> GetByNameAndConnectionCodeAsync(string name, string connectionCode, CancellationToken cancellationToken = default)
        => Task.FromResult<InteractionTable?>(null);

    public Task<IReadOnlyList<InteractionTable>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InteractionTable>>([]);

    public override string ToString() => _message;
}

public sealed class UnavailableCalculationResultRepository : ICalculationResultRepository
{
    public Task AddAsync(CalculationResult result, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<CalculationResult>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CalculationResult>>([]);

    public Task<CalculationResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<CalculationResult?>(null);
}

public sealed class UnavailableNodeImageRepository : INodeImageRepository
{
    public Task<IReadOnlyList<(string Filename, byte[] Data)>> GetByFilenamesAsync(
        IEnumerable<string> filenames,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<(string Filename, byte[] Data)>>([]);
}
