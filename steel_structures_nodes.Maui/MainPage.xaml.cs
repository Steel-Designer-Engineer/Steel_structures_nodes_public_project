using Steel_structures_nodes_public_project.Maui.ViewModels;

namespace Steel_structures_nodes_public_project.Maui;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.ChartUpdated += () =>
        {
            capacityChartView.Invalidate();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainViewModel vm)
        {
            await vm.LoadNamesCommand.ExecuteAsync(null);
        }
    }
}
