using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace steel_structures_nodes.Wpf.Services;

public interface IWpfNodeImageService
{
    Task<List<ImageSource>> LoadAllNodeImagesAsync(string nodeCode, CancellationToken cancellationToken = default);
}
