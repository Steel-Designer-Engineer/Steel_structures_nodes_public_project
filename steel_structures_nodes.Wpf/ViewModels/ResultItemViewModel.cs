using steel_structures_nodes.Wpf.Mvvm;

namespace steel_structures_nodes.Wpf.ViewModels
{
    /// <summary>
    /// ViewModel СЌР»РµРјРµРЅС‚Р° СЂРµР·СѓР»СЊС‚Р°С‚Р° (РїР°СЂР° В«РєР»СЋС‡ вЂ” Р·РЅР°С‡РµРЅРёРµВ») РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РІ СЃРїРёСЃРєРµ.
    /// </summary>
    public sealed class ResultItemViewModel : ViewModelBase
    {
        private string _key;
        private string _value;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }
    }
}
