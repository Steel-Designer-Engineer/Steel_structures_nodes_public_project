using Steel_structures_nodes_public_project.Domain.Entities;

namespace Steel_structures_nodes_public_project.Domain.Repositories;

/// <summary>
/// Репозиторий примечаний к узлам.
/// </summary>
public interface INodeNoteRepository
{
    Task<IReadOnlyList<NodeNote>> GetByNodeNameAsync(string nodeName, CancellationToken cancellationToken = default);
    Task AddAsync(NodeNote note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
