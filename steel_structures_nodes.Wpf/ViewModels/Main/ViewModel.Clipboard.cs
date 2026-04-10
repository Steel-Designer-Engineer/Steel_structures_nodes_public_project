using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using steel_structures_nodes.Calculate.Models.RSN;
using steel_structures_nodes.Calculate.Models.RSU;
using steel_structures_nodes.Wpf.Services;
namespace steel_structures_nodes.Wpf.ViewModels;

public partial class ViewModel
{    private void PasteRsu() => PasteRowsFromClipboard(RsuRows, "РСУ добавлены");

    private void PasteRsn() => PasteRowsFromClipboard(RsnRows, "РСН добавлены");

    private void AddRsu() => AddRowsFromClipboardAndSetStatus(RsuRows, "RSU added");

    private void AddRsn() => AddRowsFromClipboardAndSetStatus(RsnRows, "RSN added");

    private void CopyIdeaToClipboard()
    {
        if (IdeaRows.Count == 0)
        {
            Status = "Нет данных для копирования";
            return;
        }

        CopyTextToClipboard(BuildIdeaClipboardText(), "Данные IDEA StatiCA скопированы в буфер обмена");
    }

    private void CopyAnalysisToClipboard()
    {
        if (AnalysisRows.Count == 0)
        {
            Status = "Нет данных для копирования";
            return;
        }

        CopyTextToClipboard(BuildAnalysisClipboardText(), "Копировать таблицу");
    }

    private void CopyResultsToClipboard()
    {
        if (Results.Count == 0)
        {
            Status = "Нет данных для копирования";
            return;
        }

        CopyTextToClipboard(BuildResultsClipboardText(), "Сводка результатов скопирована в буфер обмена");
    }

    private void CopyNodeDataToClipboard()
    {
        var node = StandardNode;
        if (node == null || string.IsNullOrWhiteSpace(node.ProfileBeam))
        {
            Status = "Нет данных узла для копирования";
            return;
        }

        CopyTextToClipboard(BuildNodeClipboardText(node), "Данные узла скопированы в буфер обмена");
    }

    private void PasteRowsFromClipboard(ObservableCollection<RsuRow> target, string statusMessage)
    {
        ReplaceRowsFromClipboard(target, System.Windows.Clipboard.GetText());
        Status = statusMessage;
    }

    private void PasteRowsFromClipboard(ObservableCollection<RsnRow> target, string statusMessage)
    {
        ReplaceRowsFromClipboard(target, System.Windows.Clipboard.GetText());
        Status = statusMessage;
    }

    private void AddRowsFromClipboardAndSetStatus(ObservableCollection<RsuRow> target, string statusMessage)
    {
        AddRowsFromClipboard(target, System.Windows.Clipboard.GetText());
        Status = statusMessage;
    }

    private void AddRowsFromClipboardAndSetStatus(ObservableCollection<RsnRow> target, string statusMessage)
    {
        AddRowsFromClipboard(target, System.Windows.Clipboard.GetText());
        Status = statusMessage;
    }

    private string BuildIdeaClipboardText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Критерий\tN, kN\tVy, kN\tVz, kN\tMx, kN*m\tMy, kN*m\tMz, kN*m");

        foreach (var row in IdeaRows)
        {
            builder.Append(row.RowType ?? "");
            builder.Append('\t').Append(FmtCell(row.N));
            builder.Append('\t').Append(FmtCell(row.Vy));
            builder.Append('\t').Append(FmtCell(row.Vz));
            builder.Append('\t').Append(FmtCell(row.Mx));
            builder.Append('\t').Append(FmtCell(row.My));
            builder.Append('\t').Append(FmtCell(row.Mz));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string BuildAnalysisClipboardText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Критерий\tКомбинации нагрузок\tЭлемент\tN, кН\tNt, кН\tNc, кН\tQy, кН\tQz, кН\tMx, кН*м\tMy, кН*м\tMz, кН*м\tMw, кН*м?\tu\t?");

        foreach (var row in AnalysisRows)
        {
            builder.Append(row.RowType ?? "");
            builder.Append('\t').Append(row.LoadCombination ?? "");
            builder.Append('\t').Append(row.ElementText ?? "");
            builder.Append('\t').Append(FmtCell(row.N));
            builder.Append('\t').Append(FmtCell(row.Nt));
            builder.Append('\t').Append(FmtCell(row.Nc));
            builder.Append('\t').Append(FmtCell(row.Qy));
            builder.Append('\t').Append(FmtCell(row.Qz));
            builder.Append('\t').Append(FmtCell(row.Mx));
            builder.Append('\t').Append(FmtCell(row.My));
            builder.Append('\t').Append(FmtCell(row.Mz));
            builder.Append('\t').Append(FmtCell(row.Mw));
            builder.Append('\t').Append(FmtCell(row.U));
            builder.Append('\t').Append(FmtCell(row.Psi));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string BuildResultsClipboardText()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Параметр\tЗначение");
        foreach (var row in Results)
            builder.AppendLine($"{row.Key ?? ""}\t{row.Value ?? ""}");

        return builder.ToString();
    }

    private string BuildNodeClipboardText(StandardNodeInteractionViewModel node)
    {
        var builder = new StringBuilder();

        void AppendValue(string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value != "0")
                builder.AppendLine($"{label}\t{value}");
        }

        if (!string.IsNullOrWhiteSpace(node.TableBrand))
            builder.AppendLine($"Марка таблицы\t{node.TableBrand}");

        builder.AppendLine("--- Геометрия балки ---");
        AppendValue("Профиль балки", node.ProfileBeam);
        AppendValue("H, мм", node.BeamH); AppendValue("B, мм", node.BeamB);
        AppendValue("s, мм", node.BeamS); AppendValue("t, мм", node.BeamT);
        AppendValue("A, см?", node.BeamA); AppendValue("P, кг/м", node.BeamP);
        AppendValue("Ix, см?", node.BeamIx); AppendValue("Iy, см?", node.BeamIy); AppendValue("Iz, см?", node.BeamIz);
        AppendValue("Wy, см?", node.BeamWy); AppendValue("Wz, см?", node.BeamWz); AppendValue("Wx, см?", node.BeamWx);
        AppendValue("Sy, см?", node.BeamSy); AppendValue("Sz, см?", node.BeamSz);
        AppendValue("iy, см", node.Beamiy); AppendValue("iz, см", node.Beamiz); AppendValue("xo, см", node.BeamXo);

        builder.AppendLine("--- Геометрия колонны ---");
        AppendValue("Профиль колонны", node.ProfileColumn);
        AppendValue("H, мм", node.ColumnH); AppendValue("B, мм", node.ColumnB);
        AppendValue("s, мм", node.ColumnS); AppendValue("t, мм", node.ColumnT);
        AppendValue("A, см?", node.ColumnA); AppendValue("P, кг/м", node.ColumnP);
        AppendValue("Ix, см?", node.ColumnIx); AppendValue("Iy, см?", node.ColumnIy); AppendValue("Iz, см?", node.ColumnIz);
        AppendValue("Wy, см?", node.ColumnWy); AppendValue("Wz, см?", node.ColumnWz); AppendValue("Wx, см?", node.ColumnWx);
        AppendValue("Sy, см?", node.ColumnSy); AppendValue("Sz, см?", node.ColumnSz);
        AppendValue("iy, см", node.Columniy); AppendValue("iz, см", node.Columniz);
        AppendValue("xo, см", node.ColumnXo); AppendValue("yo, см", node.ColumnYo);

        builder.AppendLine("--- Пластина / Фланец / Рёбра ---");
        AppendValue("Пластина H, мм", node.PlateH); AppendValue("Пластина B, мм", node.PlateB); AppendValue("Пластина t, мм", node.PlateT);
        AppendValue("Фланец H, мм", node.FlangeH); AppendValue("Фланец B, мм", node.FlangeB);
        AppendValue("Фланец t, мм", node.FlangeT); AppendValue("Фланец Lb, мм", node.FlangeLb);
        AppendValue("tr1, мм", node.StiffTr1); AppendValue("tr2, мм", node.StiffTr2);
        AppendValue("tbp, мм", node.StiffTbp); AppendValue("tg, мм", node.StiffTg);
        AppendValue("tf, мм", node.StiffTf); AppendValue("twp, мм", node.StiffTwp);
        AppendValue("Lh, мм", node.StiffLh); AppendValue("Hh, мм", node.StiffHh);

        builder.AppendLine("--- Несущая способность ---");
        AppendValue("Nt, кН", node.Nt); AppendValue("Nc, кН", node.Nc); AppendValue("N, кН", node.N);
        AppendValue("Mneg, кН·м", node.Mneg);
        AppendValue("Qy, кН", node.Qy); AppendValue("Qz, кН", node.Qz);
        AppendValue("Mx, кН·м", node.Mx); AppendValue("My, кН·м", node.My);
        AppendValue("Mz, кН·м", node.Mz); AppendValue("Mw, кН·м?", node.Mw);

        builder.AppendLine("--- Жёсткость ---");
        AppendValue("Sj, кН·м/рад", node.Sj); AppendValue("Sjo, кН·м/рад", node.Sjo); AppendValue("var", node.Variable);

        builder.AppendLine("--- Коэффициенты взаимодействия ---");
        AppendValue("?", node.Alpha); AppendValue("?", node.Beta); AppendValue("?", node.Gamma);
        AppendValue("?", node.Delta); AppendValue("?", node.Epsilon); AppendValue("?", node.Lambda);

        builder.AppendLine("--- Болты ---");
        AppendValue("Диаметр, мм", node.BoltDiameter); AppendValue("Кол-во болтов", node.BoltCount);
        AppendValue("Число рядов", node.BoltRows); AppendValue("Версия", node.BoltVersion);
        AppendValue("Коорд. Y (e1,p1…)", node.BoltCoordY);
        AppendValue("Коорд. X (d1,d2)", node.BoltCoordX);
        AppendValue("Коорд. Z", node.BoltCoordZ);

        builder.AppendLine("--- Сварка ---");
        AppendValue("Катеты швов kf", node.WeldKf);

        return builder.ToString();
    }

    private void CopyTextToClipboard(string text, string statusMessage)
    {
        System.Windows.Clipboard.SetText(text);
        Status = statusMessage;
    }

    private static string FmtCell(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("0.#####", CultureInfo.InvariantCulture)
            : string.Empty;
    }

    private static void ReplaceRowsFromClipboard(ObservableCollection<RsnRow> target, string text)
    {
        target.Clear();
        AddRowsFromClipboard(target, text);
    }

    private static void AddRowsFromClipboard(ObservableCollection<RsnRow> target, string text)
    {
        foreach (var row in ClipboardRowParser.ParseRows(text))
            target.Add(row);
    }

    private static void ReplaceRowsFromClipboard(ObservableCollection<RsuRow> target, string text)
    {
        target.Clear();
        AddRowsFromClipboard(target, text);
    }

    private static void AddRowsFromClipboard(ObservableCollection<RsuRow> target, string text)
    {
        foreach (var row in ClipboardRowParser.ParseRows(text))
        {
            target.Add(new RsuRow
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
                Mw = row.Mw
            });
        }
    }
}
