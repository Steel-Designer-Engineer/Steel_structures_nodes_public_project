using steel_structures_nodes.Maui.ViewModels;

namespace steel_structures_nodes.Maui;

public partial class ExcelImportPage : ContentPage
{
    public ExcelImportPage(ExcelImportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
