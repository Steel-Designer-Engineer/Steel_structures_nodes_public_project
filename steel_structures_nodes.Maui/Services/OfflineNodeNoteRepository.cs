using Steel_structures_nodes_public_project.Domain.Entities;
using Steel_structures_nodes_public_project.Domain.Repositories;

namespace Steel_structures_nodes_public_project.Maui.Services;

/// <summary>
/// Заглушка репозитория примечаний для offline-режима.
/// </summary>
internal sealed class OfflineNodeNoteRepository : INodeNoteRepository
{
    public Task<IReadOnlyList<NodeNote>> GetByNodeNameAsync(
        string nodeName, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<NodeNote>>(Array.Empty<NodeNote>());

    public Task AddAsync(NodeNote note, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
