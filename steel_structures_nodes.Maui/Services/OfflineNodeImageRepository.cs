using Steel_structures_nodes_public_project.Domain.Repositories;

namespace Steel_structures_nodes_public_project.Maui.Services;

/// <summary>
/// Заглушка репозитория изображений для offline-режима.
/// </summary>
internal sealed class OfflineNodeImageRepository : INodeImageRepository
{
    public Task<IReadOnlyList<(string Filename, byte[] Data)>> GetByFilenamesAsync(
        IEnumerable<string> filenames,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<(string, byte[])>>([]);
}
