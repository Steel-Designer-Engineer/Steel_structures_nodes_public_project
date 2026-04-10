using System;
using System.Collections.Generic;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Calculate.Models.RSN;
using steel_structures_nodes.Calculate.Models.RSU;
using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.ViewModels;

public partial class ViewModel
{
    private void ImportRsuFromExcel() => ImportFromExcel(isRsu: true);

    private void ImportRsnFromExcel() => ImportFromExcel(isRsu: false);

    private void ImportFromExcel(bool isRsu)
    {
        try
        {
            var request = TryGetExcelImportRequest(isRsu ? "RSU" : "RSN");
            if (request == null)
                return;

            var rows = _excelReader.ReadRsuRsn(request.FilePath, request.SheetName);
            var importedCount = isRsu
                ? ImportRsuRows(rows)
                : ImportRsnRows(rows);

            Status = (isRsu ? "RSU" : "RSN") + " imported: " + importedCount;
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private ExcelImportRequest TryGetExcelImportRequest(string kind)
    {
        return _excelImportDialogService.TryGetImportRequest(kind, out var request) && request != null
            ? request
            : null;
    }

    private int ImportRsuRows(IEnumerable<ForceRow> rows)
    {
        RsuRows.Clear();
        foreach (var row in rows)
        {
            var mappedRow = MapToRsuRow(row);
            if (mappedRow != null)
                RsuRows.Add(mappedRow);
        }

        return RsuRows.Count;
    }

    private int ImportRsnRows(IEnumerable<ForceRow> rows)
    {
        RsnRows.Clear();
        foreach (var row in rows)
        {
            var mappedRow = MapToRsnRow(row);
            if (mappedRow != null)
                RsnRows.Add(mappedRow);
        }

        return RsnRows.Count;
    }

    private static RsuRow MapToRsuRow(ForceRow row)
    {
        if (row == null)
            return null;

        return row as RsuRow ?? new RsuRow
        {
            DclNo = row.DclNo,
            Elem = row.Elem,
            Sect = row.Sect,
            N = row.N,
            Mx = row.Mx,
            My = row.My,
            Qz = row.Qz,
            Mz = row.Mz,
            Qy = row.Qy,
            Mw = row.Mw,
        };
    }

    private static RsnRow MapToRsnRow(ForceRow row)
    {
        if (row == null)
            return null;

        return row as RsnRow ?? new RsnRow
        {
            DclNo = row.DclNo,
            Elem = row.Elem,
            Sect = row.Sect,
            N = row.N,
            Mx = row.Mx,
            My = row.My,
            Qz = row.Qz,
            Mz = row.Mz,
            Qy = row.Qy,
            Mw = row.Mw,
        };
    }
}
