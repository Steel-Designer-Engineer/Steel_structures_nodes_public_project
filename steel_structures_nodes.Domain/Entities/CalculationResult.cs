namespace Steel_structures_nodes_public_project.Domain.Entities;

/// <summary>
/// Результат расчёта РС1, сохраняемый в MongoDB (коллекция ResultDB).
/// </summary>
public class CalculationResult
{
    public Guid Id { get; set; }

    /// <summary>Дата/время выполнения расчёта (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Коэффициент γ_f, применённый к усилиям.</summary>
    public double GammaF { get; set; }

    /// <summary>Фильтр элементов (текст), null если не задан.</summary>
    public string? ElementFilter { get; set; }

    // ──── Сводные усилия ────
    public double? SummaryN { get; set; }
    public double? SummaryNt { get; set; }
    public double? SummaryNc { get; set; }
    public double? SummaryQy { get; set; }
    public double? SummaryQz { get; set; }
    public double? SummaryMx { get; set; }
    public double? SummaryMy { get; set; }
    public double? SummaryMz { get; set; }
    public double? SummaryMw { get; set; }
    public double? MaxU { get; set; }
    public double? SummaryPsi { get; set; }

    /// <summary>Количество строк РСУ.</summary>
    public int RsuCount { get; set; }

    /// <summary>Количество строк РСН.</summary>
    public int RsnCount { get; set; }

    /// <summary>Строки таблицы анализа.</summary>
    public List<CalculationResultAnalysisRow> AnalysisRows { get; set; } = new();
}

/// <summary>
/// Строка анализа внутри результата расчёта.
/// </summary>
public class CalculationResultAnalysisRow
{
    public string? RowType { get; set; }
    public string? LoadCombination { get; set; }
    public int? Element { get; set; }
    public double? N { get; set; }
    public double? Nt { get; set; }
    public double? Nc { get; set; }
    public double? Qy { get; set; }
    public double? Qz { get; set; }
    public double? Mx { get; set; }
    public double? My { get; set; }
    public double? Mz { get; set; }
    public double? Mw { get; set; }
    public double? U { get; set; }
    public double? Psi { get; set; }
}
