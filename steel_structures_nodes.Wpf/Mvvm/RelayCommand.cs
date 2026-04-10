using System;
using System.Windows.Input;

namespace steel_structures_nodes.Wpf.Mvvm
{
    /// <summary>
    /// Р РµР°Р»РёР·Р°С†РёСЏ <see cref="ICommand"/> РЅР° РѕСЃРЅРѕРІРµ РґРµР»РµРіР°С‚РѕРІ РґР»СЏ РїСЂРёРІСЏР·РєРё РєРѕРјР°РЅРґ РІ MVVM.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
