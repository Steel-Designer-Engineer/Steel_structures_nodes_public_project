namespace steel_structures_nodes.Maui.Services;

public interface INodeImageService
{
    Task<List<ImageSource>> LoadAllNodeImagesAsync(string nodeCode, CancellationToken cancellationToken = default);
}
