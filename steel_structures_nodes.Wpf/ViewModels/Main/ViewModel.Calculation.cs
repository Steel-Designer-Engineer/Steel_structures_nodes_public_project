using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using steel_structures_nodes.Calculate.Calculate;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Calculate.Models.RSN;
using steel_structures_nodes.Calculate.Models.RSU;

namespace steel_structures_nodes.Wpf.ViewModels;

public partial class ViewModel
{
    private void ExecuteCalculation()
    {
        try
        {
            var rsu = GetCalculationRows<RsuRow>(RsuRows);
            var rsn = GetCalculationRows<RsnRow>(RsnRows);

            if (!HasCalculationInput(rsu, rsn))
                return;

            var gammaF = ParseCoeff(_gammaF);
            ApplyGammaFactor(ref rsu, ref rsn, gammaF);

            var elementFilter = GetElementFilterOrNull();
            var createdFilePath = CalculateAndLoadResult(rsu, rsn, elementFilter);
            var calcStatus = BuildCalculationStatusMessage(createdFilePath, rsu.Count, rsn.Count);

            FinalizeCalculation(createdFilePath, calcStatus, gammaF, rsu.Count, rsn.Count, elementFilter);
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
    }

    private static List<ForceRow> GetCalculationRows<TRow>(ObservableCollection<TRow> rows)
        where TRow : ForceRow
    {
        return (rows ?? [])
            .Where(row => row != null)
            .Cast<ForceRow>()
            .ToList();
    }

    private bool HasCalculationInput(IReadOnlyCollection<ForceRow> rsu, IReadOnlyCollection<ForceRow> rsn)
    {
        if (rsu.Count != 0 || rsn.Count != 0)
            return true;

        Status = "Нет данных РСУ и РСН для расчёта";
        return false;
    }

    private void ApplyGammaFactor(ref List<ForceRow> rsu, ref List<ForceRow> rsn, double gammaF)
    {
        if (gammaF == 1d)
            return;

        rsu = ScaleForceRows(rsu, gammaF);
        rsn = ScaleForceRows(rsn, gammaF);
    }

    private HashSet<string> GetElementFilterOrNull()
    {
        return string.IsNullOrWhiteSpace(ElementFilterText)
            ? null
            : ParseElementFilter(ElementFilterText);
    }

    private string CalculateAndLoadResult(
        IReadOnlyList<ForceRow> rsu,
        IReadOnlyList<ForceRow> rsn,
        HashSet<string> elementFilter)
    {
        var calculator = new Calculator();
        var resultDir = GetResultDir();
        calculator.CalculateAndSave(rsu, rsn, elementFilter, resultDir, out var createdFilePath);

        LoadResultFromJson(createdFilePath);
        RebuildCalculationVersions();
        return createdFilePath;
    }

    private string BuildCalculationStatusMessage(string createdFilePath, int rsuCount, int rsnCount)
    {
        var parts = new List<string>();
        if (rsuCount > 0) parts.Add($"РСУ={rsuCount}");
        if (rsnCount > 0) parts.Add($"РСН={rsnCount}");

        return $"Расчёт выполнен ({string.Join(", ", parts)}), сохранён в {Path.GetFileName(createdFilePath)}";
    }

    private void FinalizeCalculation(
        string createdFilePath,
        string calcStatus,
        double gammaF,
        int rsuCount,
        int rsnCount,
        HashSet<string> elementFilter)
    {
        Status = calcStatus;

        if (_hasCalcData)
            BuildComparisonChart(_lastCalcNPlus, _lastCalcNMinus, _lastCalcQAbs, _lastCalcQzAbs, _lastCalcTAbs, _lastCalcMAbs, _lastCalcMoAbs, _lastCalcMwAbs);

        SaveResultToMongo(createdFilePath, gammaF, rsuCount, rsnCount, elementFilter);
        CalculationCompleted?.Invoke();
        UpdateStandardNodeFromJsonAsync(calcStatus);
    }
}
