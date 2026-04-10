using System.Collections.ObjectModel;
namespace steel_structures_nodes.Maui.ViewModels;

public partial class MainViewModel
{
    private async Task LoadConnectionNamesAsync()
    {
        Status = "Загрузка групп…";
        ReplaceItems(ConnectionNames, await _interactionTableLookupRepository.GetDistinctNamesAsync());
    }

    private async Task<bool> TrySelectInitialConnectionNameAsync()
    {
        if (ConnectionNames.Count == 0)
        {
            Status = "Нет данных в БД";
            return false;
        }

        await Task.Yield();
        SelectedName = ConnectionNames[0];
        UpdateSelectedNodePresentation(SelectedName);
        _ = SafeAsync(LoadNodeImagesAsync(SelectedName!));
        return true;
    }

    private async Task LoadProfileColumnsAsync(string name)
    {
        ReplaceItems(ProfileColumns, await _interactionTableLookupRepository.GetDistinctProfileColumnsByNameAsync(name));
    }

    private async Task SelectFirstProfileColumnAsync()
    {
        await Task.Yield();
        SelectedProfileColumn = ProfileColumns.Count > 0 ? ProfileColumns[0] : null;
    }

    private void SelectFirstProfileBeam()
    {
        SelectedProfileBeam = ProfileBeams.Count > 0 ? ProfileBeams[0] : null;
    }

    private async Task<IReadOnlyList<string>> GetBeamsForCurrentSelectionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedName))
            return Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(SelectedProfileColumn))
            return await _interactionTableLookupRepository.GetDistinctProfileBeamsByNameAndColumnAsync(SelectedName, SelectedProfileColumn);

        return await _interactionTableLookupRepository.GetDistinctProfileBeamsByNameAsync(SelectedName);
    }

    private async Task<IReadOnlyList<string>> GetConnectionCodesForCurrentSelectionAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedName))
            return Array.Empty<string>();

        if (!string.IsNullOrWhiteSpace(SelectedProfileColumn) && !string.IsNullOrWhiteSpace(SelectedProfileBeam))
            return await _interactionTableLookupRepository.GetConnectionCodesByNameColumnAndBeamAsync(SelectedName, SelectedProfileColumn, SelectedProfileBeam);

        if (!string.IsNullOrWhiteSpace(SelectedProfileColumn))
            return await _interactionTableLookupRepository.GetConnectionCodesByNameAndColumnAsync(SelectedName, SelectedProfileColumn);

        if (!string.IsNullOrWhiteSpace(SelectedProfileBeam))
            return await _interactionTableLookupRepository.GetConnectionCodesByNameAndBeamAsync(SelectedName, SelectedProfileBeam);

        return await _interactionTableLookupRepository.GetConnectionCodesByNameAsync(SelectedName);
    }

    private static void ReplaceItems(ObservableCollection<string> target, IEnumerable<string> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}
