using System;
using System.Collections.Generic;
using System.Globalization;
using OfficeOpenXml;

namespace Steel_structures_nodes_public_project.Calculate
{
    /// <summary>
    /// Импорт и вставка данных РСН в расчётный лист Excel.
    /// Эквивалент VBA-макросов «Вставить_РСН» и «Добавить_РСН».
    /// </summary>
    public class RsnDataProcessor
    {
        /// <summary>Начальная строка данных на листе.</summary>
        private const int DataStartRow = 22;

        /// <summary>Столбец K (DCL No / номер загружения).</summary>
        private const int ColK = 11;

        /// <summary>Столбец O (начало силовых данных).</summary>
        private const int ColO = 15;

        /// <summary>
        /// Вставляет строки РСН начиная с строки <see cref="DataStartRow"/> (K22).
        /// Существующие данные перезаписываются.
        /// </summary>
        /// <param name="ws">Рабочий лист «DCL(РСН)».</param>
        /// <param name="rows">Данные для вставки (каждая строка — массив значений ячеек начиная с колонки K).</param>
        /// <returns>Количество вставленных строк.</returns>
        public int Insert(ExcelWorksheet ws, IReadOnlyList<string[]> rows)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            if (rows == null || rows.Count == 0) return 0;

            PasteRows(ws, DataStartRow, ColK, rows);

            int lastRow = FindLastDataRow(ws, ColO, DataStartRow);
            SetRowCount(ws, lastRow + 1);

            return rows.Count;
        }

        /// <summary>
        /// Добавляет строки РСН после последней заполненной строки.
        /// Эквивалент VBA «Добавить_РСН».
        /// </summary>
        /// <param name="ws">Рабочий лист «DCL(РСН)».</param>
        /// <param name="rows">Данные для добавления.</param>
        /// <returns>Количество добавленных строк.</returns>
        public int Append(ExcelWorksheet ws, IReadOnlyList<string[]> rows)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            if (rows == null || rows.Count == 0) return 0;

            int existingLastRow = FindLastDataRow(ws, ColO, DataStartRow - 1);
            int insertRow = existingLastRow + 1;

            PasteRows(ws, insertRow, ColK, rows);

            int newLastRow = FindLastDataRow(ws, ColO, DataStartRow - 1);
            SetRowCount(ws, newLastRow + 1);

            return rows.Count;
        }

        /// <summary>
        /// Записывает строки в лист начиная с указанной позиции.
        /// </summary>
        private static void PasteRows(ExcelWorksheet ws, int startRow, int startCol, IReadOnlyList<string[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                for (int j = 0; j < row.Length; j++)
                {
                    ws.Cells[startRow + i, startCol + j].Value = row[j];
                }
            }
        }

        /// <summary>
        /// Находит последнюю заполненную строку в указанном столбце, начиная с <paramref name="fromRow"/>.
        /// </summary>
        private static int FindLastDataRow(ExcelWorksheet ws, int col, int fromRow)
        {
            int last = fromRow;
            int maxRow = ws.Dimension?.End.Row ?? fromRow;

            for (int r = fromRow; r <= maxRow; r++)
            {
                var val = ws.Cells[r, col].Value;
                if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    last = r;
            }

            return last;
        }

        /// <summary>
        /// TODO. Не правильно! Записывает количество строк в ячейку I19 (используется формулами листа).
        /// </summary>
        private static void SetRowCount(ExcelWorksheet ws, int value)
        {
            ws.Cells[19, 9].Value = value; // I19
        }
    }
}
