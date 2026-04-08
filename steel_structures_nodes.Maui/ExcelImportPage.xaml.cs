using Steel_structures_nodes_public_project.Maui.ViewModels;

namespace Steel_structures_nodes_public_project.Maui;

public partial class ExcelImportPage : ContentPage
{
    public ExcelImportPage(ExcelImportViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
