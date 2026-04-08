using System;
using System.Globalization;
using OfficeOpenXml;

namespace Steel_structures_nodes_public_project.Calculate
{
    /// <summary>
    /// Форматирование данных расчётного листа: замена запятых на точки
    /// и заполнение формул с последующей фиксацией значений.
    /// Эквивалент VBA-макросов «Замена_запятой» и «Заполнить_таблицу2».
    /// </summary>
    public class DclSheetFormatter
    {
        private const int DataStartRow = 21;
        private const int FormulaRow = 21;

        /// <summary>Столбец O (начало силовых данных).</summary>
        private const int ColO = 15;

        /// <summary>Столбец U (конец силовых данных).</summary>
        private const int ColU = 21;

        /// <summary>Столбец W (начало расчётных формул).</summary>
        private const int ColW = 23;

        /// <summary>Столбец AN (конец расчётных формул).</summary>
        private const int ColAN = 40;

        /// <summary>
        /// Заменяет запятые на точки в диапазоне силовых данных O21:U{lastRow}.
        /// Эквивалент VBA «Замена_запятой».
        /// </summary>
        /// <param name="ws">Рабочий лист.</param>
        public void ReplaceCommasWithDots(ExcelWorksheet ws)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));

            int lastRow = GetLastDataRow(ws);

            for (int r = DataStartRow; r <= lastRow; r++)
            {
                for (int c = ColO; c <= ColU; c++)
                {
                    var val = ws.Cells[r, c].Value;
                    if (val == null) continue;

                    var text = val.ToString();
                    if (text.Contains(","))
                    {
                        var normalized = text.Replace(',', '.');
                        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                            ws.Cells[r, c].Value = d;
                        else
                            ws.Cells[r, c].Value = normalized;
                    }
                }
            }
        }

        /// <summary>
        /// Заполняет формулы из строки 21 вниз (W21:AN21 → W22:AN{lastRow}),
        /// затем заменяет формулы вычисленными значениями.
        /// Эквивалент VBA «Заполнить_таблицу2».
        /// </summary>
        /// <param name="ws">Рабочий лист.</param>
        /// <remarks>
        /// Шаги:
        /// 1. Замена запятых на точки в O22:U{lastRow}
        /// 2. Копирование формул из W21:AN21 в строки 22..lastRow
        /// 3. Пересчёт и фиксация значений (вместо формул)
        /// </remarks>
        public void FillTableAndFixValues(ExcelWorksheet ws)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));

            int lastRow = GetLastDataRow(ws);
            if (lastRow <= FormulaRow) return;

            // 1. Замена запятых в области данных (O22:U)
            for (int r = DataStartRow + 1; r <= lastRow; r++)
            {
                for (int c = ColO; c <= ColU; c++)
                {
                    var val = ws.Cells[r, c].Value;
                    if (val == null) continue;

                    var text = val.ToString();
                    if (text.Contains(","))
                    {
                        var normalized = text.Replace(',', '.');
                        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                            ws.Cells[r, c].Value = d;
                        else
                            ws.Cells[r, c].Value = normalized;
                    }
                }
            }

            // 2. Автозаполнение формул W21:AN21 → W22:AN{lastRow}
            for (int c = ColW; c <= ColAN; c++)
            {
                var sourceFormula = ws.Cells[FormulaRow, c].Formula;
                if (string.IsNullOrWhiteSpace(sourceFormula))
                {
                    // Если нет формулы — копируем значение
                    var sourceValue = ws.Cells[FormulaRow, c].Value;
                    for (int r = FormulaRow + 1; r <= lastRow; r++)
                    {
                        ws.Cells[r, c].Value = sourceValue;
                    }
                }
                else
                {
                    // Копируем формулу со сдвигом строки
                    for (int r = FormulaRow + 1; r <= lastRow; r++)
                    {
                        ws.Cells[FormulaRow, c, FormulaRow, c]
                            .Copy(ws.Cells[r, c]);
                    }
                }
            }

            // 3. Пересчёт и фиксация значений (PasteSpecial xlPasteValues)
            ws.Calculate();

            for (int r = FormulaRow + 1; r <= lastRow; r++)
            {
                for (int c = ColW; c <= ColAN; c++)
                {
                    var cell = ws.Cells[r, c];
                    if (!string.IsNullOrWhiteSpace(cell.Formula))
                    {
                        var computedValue = cell.Value;
                        cell.Formula = string.Empty;
                        cell.Value = computedValue;
                    }
                }
            }
        }

        /// <summary>
        /// Определяет последнюю строку данных по столбцу O.
        /// </summary>
        private static int GetLastDataRow(ExcelWorksheet ws)
        {
            int lastRow = DataStartRow;
            int maxRow = ws.Dimension?.End.Row ?? DataStartRow;

            for (int r = DataStartRow; r <= maxRow; r++)
            {
                var val = ws.Cells[r, ColO].Value;
                if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    lastRow = r;
            }

            // Записать в I19
            ws.Cells[19, 9].Value = lastRow;

            return lastRow;
        }
    }
}
