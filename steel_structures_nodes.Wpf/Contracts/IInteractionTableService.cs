using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.Services
{
    public interface IInteractionTableService
    {
        string[] LoadDistinctNames();
        string[] LoadConnectionCodesByName(string name);
        string[] LoadConnectionCodesByNameAndBeam(string name, string beam);
        string[] LoadConnectionCodesByNameAndColumn(string name, string column);
        string[] LoadConnectionCodesByNameColumnAndBeam(string name, string column, string beam);
        StandardNodeData LoadStandardNode(string name, string connectionCode);
        string[] LoadDistinctProfileBeamsByName(string name);
        string[] LoadDistinctProfileBeamsByNameAndColumn(string name, string column);
        string[] LoadDistinctProfileColumnsByName(string name);
    }
}
