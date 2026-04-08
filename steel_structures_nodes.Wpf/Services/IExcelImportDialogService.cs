using Steel_structures_nodes_public_project.Wpf.Models;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// РРЅС‚РµСЂС„РµР№СЃ СЃРµСЂРІРёСЃР° РґРёР°Р»РѕРіР° РёРјРїРѕСЂС‚Р° Excel: РѕС‚РєСЂС‹С‚РёРµ С„Р°Р№Р»Р° Рё РІС‹Р±РѕСЂ Р»РёСЃС‚Р°.
    /// </summary>
    public interface IExcelImportDialogService
    {
        bool TryGetImportRequest(string kind, out ExcelImportRequest request);
    }
}
