using steel_structures_nodes.Domain.Entities;
using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.Services
{
    internal interface IStandardNodeDataMapper
    {
        StandardNodeData Map(InteractionTable table);
    }
}
