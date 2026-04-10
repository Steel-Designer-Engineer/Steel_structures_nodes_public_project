using System.Globalization;
using steel_structures_nodes.Wpf.Mvvm;

namespace steel_structures_nodes.Wpf.ViewModels
{
    /// <summary>
    /// ViewModel СЃС‚СЂРѕРєРё С‚Р°Р±Р»РёС†С‹ Р°РЅР°Р»РёР·Р° Р РЎ1 РґР»СЏ РїСЂРёРІСЏР·РєРё РґР°РЅРЅС‹С… РІ UI.
    /// </summary>
    public sealed class Rs1AnalysisRowViewModel : ViewModelBase
    {
        public string RowType { get; set; }
        public string LoadCombination { get; set; }
        public int? Element { get; set; }

        public double? Qy { get; set; }
        public double? N { get; set; }
        public double? Qz { get; set; }
        public double? Mx { get; set; }
        public double? My { get; set; }
        public double? Mz { get; set; }
        public double? Mw { get; set; }
        public double? Nt { get; set; }
        public double? Nc { get; set; }
        public double? U { get; set; }
        public double? Psi { get; set; }

        public string ElementText => Element.HasValue ? Element.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }
}
