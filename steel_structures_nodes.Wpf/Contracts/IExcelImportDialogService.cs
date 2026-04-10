using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.Services
{
    /// <summary>
    /// РРЅС‚РµСЂС„РµР№СЃ СЃРµСЂРІРёСЃР° РґРёР°Р»РѕРіР° РёРјРїРѕСЂС‚Р° Excel: РѕС‚РєСЂС‹С‚РёРµ С„Р°Р№Р»Р° Рё РІС‹Р±РѕСЂ Р»РёСЃС‚Р°.
    /// </summary>
    public interface IExcelImportDialogService
    {
        bool TryGetImportRequest(string kind, out ExcelImportRequest request);
    }
}
