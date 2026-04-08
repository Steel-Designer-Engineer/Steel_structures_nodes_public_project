using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Steel_structures_nodes_public_project.Calculate.Services;
using Steel_structures_nodes_public_project.Wpf.Models;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// РЎРµСЂРІРёСЃ РґРёР°Р»РѕРіР° РёРјРїРѕСЂС‚Р° Excel: РѕС‚РєСЂС‹РІР°РµС‚ С„Р°Р№Р»РѕРІС‹Р№ РґРёР°Р»РѕРі Рё РїСЂРµРґР»Р°РіР°РµС‚ РІС‹Р±СЂР°С‚СЊ Р»РёСЃС‚.
    /// </summary>
    public sealed class ExcelImportDialogService : IExcelImportDialogService
    {
        public bool TryGetImportRequest(string kind, out ExcelImportRequest request)
        {
            request = null;

            var path = SelectExcelFile();
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var sheet = PromptSheetName(kind, Application.Current?.MainWindow, path);
            if (string.IsNullOrWhiteSpace(sheet))
                return false;

            request = new ExcelImportRequest { FilePath = path, SheetName = sheet };
            return true;
        }

        private static string SelectExcelFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx;*.xlsm;*.xls)|*.xlsx;*.xlsm;*.xls|All Files (*.*)|*.*",
                Multiselect = false
            };

            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private static string PromptSheetName(string kind, Window owner, string filePath)
        {
            // Keep previous behavior: manual input. Some environments have issues reading workbook metadata.
            string hint = null;
            try
            {
                var reader = new EpplusExcelReader();
                var info = reader.GetWorkbookInfo(filePath);
                if (info?.SheetNames != null && info.SheetNames.Count > 0)
                    hint = string.Join(", ", info.SheetNames.Where(n => !string.IsNullOrWhiteSpace(n)).Take(8));
            }
            catch
            {
                // ignore, keep manual input
            }

            var w = new Window
            {
                Title = "Sheet name",
                Width = 520,
                Height = hint != null ? 210 : 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                ShowInTaskbar = false,
                Owner = owner
            };

            var grid = new System.Windows.Controls.Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            if (hint != null)
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

            var lbl = new System.Windows.Controls.TextBlock { Text = "Enter Excel sheet name for " + kind + ":" };
            System.Windows.Controls.Grid.SetRow(lbl, 0);

            var tb = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 8, 0, 8) };
            System.Windows.Controls.Grid.SetRow(tb, 1);

            System.Windows.Controls.TextBlock hintBlock = null;
            int buttonsRow = 2;
            if (hint != null)
            {
                hintBlock = new System.Windows.Controls.TextBlock
                {
                    Text = "Available sheets: " + hint,
                    Opacity = 0.75,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                System.Windows.Controls.Grid.SetRow(hintBlock, 2);
                buttonsRow = 3;
            }

            var buttons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var ok = new System.Windows.Controls.Button { Content = "OK", MinWidth = 75, IsDefault = true };
            var cancel = new System.Windows.Controls.Button { Content = "Cancel", MinWidth = 75, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            System.Windows.Controls.Grid.SetRow(buttons, buttonsRow);

            ok.Click += (s, e) => { w.DialogResult = true; w.Close(); };

            grid.Children.Add(lbl);
            grid.Children.Add(tb);
            if (hintBlock != null) grid.Children.Add(hintBlock);
            grid.Children.Add(buttons);
            w.Content = grid;

            return w.ShowDialog() == true ? tb.Text : null;
        }
    }
}
