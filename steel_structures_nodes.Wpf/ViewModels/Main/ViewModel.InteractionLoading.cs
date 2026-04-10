using System.Collections.ObjectModel;
using System.Linq;

namespace steel_structures_nodes.Wpf.ViewModels;

public partial class ViewModel
{
    private void RebuildConnectionCodeItemsFromInteractionTables()
    {
        var codes = LoadConnectionCodesFromInteractionTables();
        ReplaceItems(ConnectionCodeItems, codes);
        UpdateSelectedConnectionCode();
    }

    private string[] LoadConnectionCodesFromInteractionTables()
    {
        try
        {
            var hasColumn = !string.IsNullOrWhiteSpace(ElementSectionColumn);
            var hasBeam = !string.IsNullOrWhiteSpace(ElementSectionBeam);

            if (hasColumn && hasBeam)
                return _interactionService.LoadConnectionCodesByNameColumnAndBeam(ConnectionName, ElementSectionColumn, ElementSectionBeam);

            if (hasColumn)
                return _interactionService.LoadConnectionCodesByNameAndColumn(ConnectionName, ElementSectionColumn);

            if (hasBeam)
                return _interactionService.LoadConnectionCodesByNameAndBeam(ConnectionName, ElementSectionBeam);

            return _interactionService.LoadConnectionCodesByName(ConnectionName);
        }
        catch
        {
            return [];
        }
    }

    private void UpdateSelectedConnectionCode()
    {
        _standardConnectionCode = ConnectionCodeItems.Count > 0
            ? ConnectionCodeItems[0]
            : string.Empty;

        OnPropertyChanged(nameof(StandardConnectionCode));
    }

    private void RebuildProfileListFromInteractionTables()
    {
        ReplaceItems(ElementSectionsColumn, LoadProfileColumnsFromInteractionTables());
        SelectFirstColumnSection();
        RebuildBeamListByColumn();
    }

    private string[] LoadProfileColumnsFromInteractionTables()
    {
        try
        {
            return _interactionService.LoadDistinctProfileColumnsByName(ConnectionName);
        }
        catch
        {
            return [];
        }
    }

    private void SelectFirstColumnSection()
    {
        _elementSectionColumn = ElementSectionsColumn.Count > 0
            ? ElementSectionsColumn[0]
            : string.Empty;

        OnPropertyChanged(nameof(ElementSectionColumn));
    }

    private void RebuildBeamListByColumn()
    {
        ReplaceItems(ElementSectionsBeam, LoadBeamSectionsByColumn());
        SelectFirstBeamSection();
    }

    private string[] LoadBeamSectionsByColumn()
    {
        try
        {
            return _interactionService.LoadDistinctProfileBeamsByNameAndColumn(ConnectionName, ElementSectionColumn);
        }
        catch
        {
            return [];
        }
    }

    private void SelectFirstBeamSection()
    {
        _elementSectionBeam = ElementSectionsBeam.Count > 0
            ? ElementSectionsBeam[0]
            : string.Empty;

        OnPropertyChanged(nameof(ElementSectionBeam));
    }

    private static void ReplaceItems(ObservableCollection<string> target, string[] items)
    {
        target.Clear();
        foreach (var item in items.Where(x => !string.IsNullOrWhiteSpace(x)))
            target.Add(item);
    }
}
