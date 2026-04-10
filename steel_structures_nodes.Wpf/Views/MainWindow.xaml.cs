using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using steel_structures_nodes.Wpf.ViewModels;

namespace steel_structures_nodes.Wpf.Views
{
    /// <summary>
    /// Главное окно приложения
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(ViewModel viewModel)
        {
            InitializeComponent();
            Icon = new BitmapImage(new Uri("pack://application:,,,/appicon.png"));
            DataContext = viewModel;
            StateChanged += MainWindow_StateChanged;
            viewModel.CalculationCompleted += () =>
                Dispatcher.BeginInvoke(() => MainScrollViewer.ScrollToEnd());
        }

        private ViewModel Vm => DataContext as ViewModel;

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (Vm != null)
                Vm.CurrentWindowState = WindowState;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove throws if mouse button is released before the call completes
                }
            }
        }

        private void TryExecute(ICommand command)
        {
            if (command == null)
            {
                MessageBox.Show("Command is not bound (DataContext issue)", "RS1", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (command.CanExecute(null))
                command.Execute(null);
        }

        private void ImportRsu_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Command != null) return;
            TryExecute(Vm?.ImportRsuFromExcelCommand);
        }

        private void ImportRsn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Command != null) return;
            TryExecute(Vm?.ImportRsnFromExcelCommand);
        }

        /// <summary>
        /// Копирует значение поля в буфер обмена.
        /// Вызывается из ControlTemplate стиля SelectableValue.
        /// </summary>
        private void CopyValue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string text && !string.IsNullOrEmpty(text))
                Clipboard.SetText(text);
        }
    }
}
