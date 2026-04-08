using System.Globalization;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate;

namespace Steel_structures_nodes_public_project.Calculate.Models
{
    /// <summary>
    /// Базовая модель строки силовых данных, импортированных из Excel (или полученная расчётом для вывода).
    /// Может быть как РСУ, так и РСН: идентификатор загружения, элемент, сечение и усилия.
    /// Свойства Parsed* возвращают строковые значения, разобранные в числа.
    /// Также содержит значения несущей способности узла из альбома (P1–P6),
    /// сводочные значения и результаты расчёта РС1.
    /// </summary>
    public class ForceRow
    {
        /// <summary>Идентификатор загружения/комбинации нагрузок (DCL No).</summary>
        public string DclNo { get; set; }

        /// <summary>Номер элемента.</summary>
        public string Elem { get; set; }

        /// <summary>Номер сечения элемента (тип и числовой номер из Excel).</summary>
        public string Sect { get; set; }

        /// <summary>Продольная сила N (строковое значение из Excel).</summary>
        public string N { get; set; }

        /// <summary>Крутящий момент Mx (строковое значение из Excel).</summary>
        public string Mx { get; set; }

        /// <summary>Изгибающий момент My (строковое значение из Excel).</summary>
        public string My { get; set; }

        /// <summary>Поперечная сила Qz (строковое значение из Excel).</summary>
        public string Qz { get; set; }

        /// <summary>Изгибающий момент Mz (строковое значение из Excel).</summary>
        public string Mz { get; set; }

        /// <summary>Поперечная сила Qy (строковое значение из Excel).</summary>
        public string Qy { get; set; }

        /// <summary>Бимомент Mw (строковое значение из Excel).</summary>
        public string Mw { get; set; }

        /// <summary>Числовое значение N, полученное разбором строки.</summary>
        public double? ParsedN => ParseValueInDouble(N);

        /// <summary>Числовое значение Mx, полученное разбором строки.</summary>
        public double? ParsedMx => ParseValueInDouble(Mx);

        /// <summary>Числовое значение My, полученное разбором строки.</summary>
        public double? ParsedMy => ParseValueInDouble(My);

        /// <summary>Числовое значение Qz, полученное разбором строки.</summary>
        public double? ParsedQz => ParseValueInDouble(Qz);

        /// <summary>Числовое значение Mz, полученное разбором строки.</summary>
        public double? ParsedMz => ParseValueInDouble(Mz);

        /// <summary>Числовое значение Qy, полученное разбором строки.</summary>
        public double? ParsedQy => ParseValueInDouble(Qy);

        /// <summary>Числовое значение Mw, полученное разбором строки.</summary>
        public double? ParsedMw => ParseValueInDouble(Mw);

        // Значения несущей способности узла из альбома (P1–P6)
        /// <summary>Ключ строки несущей способности (например, <c>P1-P4-P6</c>).</summary>
        public string Key { get; set; }
        /// <summary>Несущая способность по сжатию AlbumNc (кН).</summary>
        public double AlbumNc { get; set; }
        /// <summary>Несущая способность по Qy (кН).</summary>
        public double AlbumQy { get; set; }
        /// <summary>Несущая способность по Qz (кН).</summary>
        public double AlbumQz { get; set; }
        /// <summary>Несущая способность по крутящему моменту Mx (кН·м).</summary>
        public double AlbumMx { get; set; }
        /// <summary>Несущая способность по изгибающему моменту Mz (кН·м).</summary>
        public double AlbumMz { get; set; }
        /// <summary>Несущая способность по бимоменту Mw (кН·м?).</summary>
        public double AlbumMw { get; set; }
        /// <summary>Несущая способность по крутящему моменту (кН·м).</summary>
        public double? AlbumT { get; set; }
        /// <summary>Несущая способность по растяжению (кН).</summary>
        public double? AlbumNt { get; set; }
        /// <summary>Несущая способность по сжатию (кН).</summary>
        public double? AlbumN { get; set; }
        /// <summary>Несущая способность по изгибу My (кН·м).</summary>
        public double? AlbumMy { get; set; }
        /// <summary>Коэффициент ? из альбома несущей способности.</summary>
        public double? AlbumPsi { get; set; }
        // Сводочные значения (из Rs1AnalysisSummary)
        /// <summary>Максимальная продольная сила N (max |N|) по таблице анализа (кН).</summary>
        public double? SummaryN { get; set; }
        /// <summary>Максимальная растягивающая сила Nt по таблице анализа (кН).</summary>
        public double? SummaryNt { get; set; }
        /// <summary>Максимальная сжимающая сила Nc по таблице анализа (кН).</summary>
        public double? SummaryNc { get; set; }
        /// <summary>Максимальное значение |Qy| по таблице анализа (кН).</summary>
        public double? SummaryQy { get; set; }
        /// <summary>Максимальное значение |Qz| по таблице анализа (кН).</summary>
        public double? SummaryQz { get; set; }
        /// <summary>Максимальное значение |Mz| по таблице анализа (кН·м).</summary>
        public double? SummaryMz { get; set; }
        /// <summary>Максимальное крутящее значение |Mx| по таблице анализа (кН·м).</summary>
        public double? SummaryMx { get; set; }
        /// <summary>Максимальное изгибающее значение |My| по таблице анализа (кН·м).</summary>
        public double? SummaryMy { get; set; }
        /// <summary>Максимальное значение бимомента |Mw| по таблице анализа (кН·м?).</summary>
        public double? SummaryMw { get; set; }
        /// <summary>Максимальный коэффициент использования u по таблице анализа.</summary>
        public double? MaxU { get; set; }
        /// <summary>Коэффициент ? по таблице анализа.</summary>
        public double? SummaryPsi { get; set; }
        // Результат расчёта РС1 (из Rs1CalculationResult)
        /// <summary>Версия расчёта (1, 2, 3, ...).</summary>
        public int Version { get; set; }

        /// <summary>Результат расчёта (MAX-строки), заполняется калькулятором.</summary>
        public List<AnalysisRow> AnalysisRows { get; set; } = new();

        /// <summary>
        /// Разбирает строковое значение в double.
        /// </summary>
        /// <param name="s">Строковое значение (может содержать запятую как разделитель).</param>
        /// <returns>Числовое значение или <c>null</c>, если разбор невозможен.</returns>
        private static double? ParseValueInDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var t = s.Trim().Replace(',', '.');
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }
    }
}
