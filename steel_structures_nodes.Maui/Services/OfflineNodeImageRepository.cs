using steel_structures_nodes.Domain.Contracts;

namespace steel_structures_nodes.Maui.Services;

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
