using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Steel_structures_nodes_public_project.Wpf.Mvvm
{
    /// <summary>
    /// Р‘Р°Р·РѕРІС‹Р№ РєР»Р°СЃСЃ ViewModel СЃ СЂРµР°Р»РёР·Р°С†РёРµР№ <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
