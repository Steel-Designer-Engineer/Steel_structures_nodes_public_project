using System;
using System.Collections.Generic;
using System.Globalization;
using OfficeOpenXml;

namespace steel_structures_nodes.Calculate.Calculate
{
    /// <summary>
    /// Импорт и вставка данных РСУ в расчётный лист Excel.
    /// Эквивалент VBA-макросов «Вставить_РСУ» и «Добавить_РСУ».
    /// 
    /// РСУ-данные поступают из буфера обмена в промежуточную область (DC–DQ),
    /// затем перемещаются в основную таблицу с перекомпоновкой столбцов:
    ///   DC:DD → AlbumMy:N  (элемент, сечение)
    ///   DI:DO → O:U  (N, MX, MY, QZ, MZ, QY, MW)
    ///   DP    → K    (номер загружения / тип КЭ)
    /// </summary>
    public class RsuDataProcessor
    {
        // Основная таблица
        private const int DataStartRow = 22;
        private const int ColK = 11;   // K — DCL No
        private const int ColM = 13;   // AlbumMy — элемент
        private const int ColO = 15;   // O — начало силовых данных

        // Промежуточная область (буфер)
        private const int TempStartRow = 21;
        private const int ColDC = 107; // DC
        private const int ColDD = 108; // DD
        private const int ColDI = 113; // DI — начало силовых данных в буфере
        private const int ColDO = 119; // DO
        private const int ColDP = 120; // DP
        private const int ColDQ = 121; // DQ — конец буфера

        /// <summary>
        /// Вставляет РСУ-данные: записывает в промежуточную область, перекомпоновывает
        /// столбцы и переносит в основную таблицу начиная со строки 22.
        /// Эквивалент VBA «Вставить_РСУ».
        /// </summary>
        /// <param name="ws">Рабочий лист «DCL(РСН)».</param>
        /// <param name="rawRows">Сырые данные из буфера обмена (колонки DC–DQ).</param>
        /// <returns>Количество обработанных строк.</returns>
        public int Insert(ExcelWorksheet ws, List<string[]> rawRows)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            if (rawRows == null || rawRows.Count == 0) return 0;

            // 1. Записать в промежуточную область
            PasteToTempArea(ws, rawRows);

            // 2. Определить количество строк
            int lastRow = FindLastDataRow(ws, ColDI, TempStartRow);
            SetRowCount(ws, lastRow + 1);

            // 3. Сдвиг столбца типа КЭ (DO → DP, если DP пуст)
            ShiftElementTypeColumn(ws, lastRow);

            // 4. Перенести данные в основную таблицу
            MoveToMainTable(ws, DataStartRow, lastRow);

            // 5. Очистить промежуточную область
            ClearTempArea(ws, lastRow);

            return rawRows.Count;
        }

        /// <summary>
        /// Добавляет РСУ-данные после последней заполненной строки основной таблицы.
        /// Эквивалент VBA «Добавить_РСУ».
        /// </summary>
        /// <param name="ws">Рабочий лист «DCL(РСН)».</param>
        /// <param name="rawRows">Сырые данные из буфера обмена.</param>
        /// <returns>Количество обработанных строк.</returns>
        public int Append(ExcelWorksheet ws, List<string[]> rawRows)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));
            if (rawRows == null || rawRows.Count == 0) return 0;

            // Определить первую свободную строку в основной таблице
            int existingLastRow = FindLastDataRow(ws, ColO, TempStartRow);
            int firstRow = existingLastRow + 1;

            // Записать I18 для отслеживания
            ws.Cells[18, 9].Value = firstRow; // I18

            // 1. Записать в промежуточную область
            PasteToTempArea(ws, rawRows);

            // 2. Определить количество строк в буфере
            int lastRow = FindLastDataRow(ws, ColDI, TempStartRow);
            SetRowCount(ws, lastRow + 1);

            // 3. Сдвиг столбца типа КЭ
            ShiftElementTypeColumn(ws, lastRow);

            // 4. Перенести в основную таблицу, начиная с firstRow
            MoveToMainTable(ws, firstRow, lastRow);

            // 5. Очистить промежуточную область
            ClearTempArea(ws, lastRow);

            return rawRows.Count;
        }

        /// <summary>
        /// Записывает сырые данные в промежуточную область (DC21 и далее).
        /// </summary>
        private static void PasteToTempArea(ExcelWorksheet ws, List<string[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                for (int j = 0; j < row.Length; j++)
                {
                    ws.Cells[TempStartRow + i, ColDC + j].Value = row[j];
                }
            }
        }

        /// <summary>
        /// Если колонка DP пуста — сдвигает данные из DO в DP.
        /// Эквивалент VBA:
        /// <code>
        /// If Range("DP21") = "" Then
        ///     Range("DO21:DO" &amp; lLastRow).Cut Range("DP21")
        /// End If
        /// </code>
        /// </summary>
        private static void ShiftElementTypeColumn(ExcelWorksheet ws, int lastRow)
        {
            var dpValue = ws.Cells[TempStartRow, ColDP].Value;
            bool dpIsEmpty = dpValue == null || string.IsNullOrWhiteSpace(dpValue.ToString());

            if (dpIsEmpty)
            {
                // Переместить DO → DP
                for (int r = TempStartRow; r <= lastRow; r++)
                {
                    ws.Cells[r, ColDP].Value = ws.Cells[r, ColDO].Value;
                    ws.Cells[r, ColDO].Value = null;
                }
            }
        }

        /// <summary>
        /// Переносит данные из промежуточной области в основную таблицу.
        ///   DC:DD → AlbumMy:N  (2 столбца: элемент, сечение)
        ///   DI:DO → O:U  (7 столбцов: N, MX, MY, QZ, MZ, QY, MW)
        ///   DP    → K    (1 столбец: номер загружения)
        /// </summary>
        private static void MoveToMainTable(ExcelWorksheet ws, int targetStartRow, int tempLastRow)
        {
            int rowCount = tempLastRow - TempStartRow + 1;

            for (int i = 0; i < rowCount; i++)
            {
                int srcRow = TempStartRow + i;
                int dstRow = targetStartRow + i;

                // DC:DD → AlbumMy:N (элемент, сечение)
                ws.Cells[dstRow, ColM].Value = ws.Cells[srcRow, ColDC].Value;
                ws.Cells[dstRow, ColM + 1].Value = ws.Cells[srcRow, ColDD].Value;

                // DI:DO → O:U (7 силовых столбцов)
                for (int c = 0; c < 7; c++)
                {
                    ws.Cells[dstRow, ColO + c].Value = ws.Cells[srcRow, ColDI + c].Value;
                }

                // DP → K (номер загружения)
                ws.Cells[dstRow, ColK].Value = ws.Cells[srcRow, ColDP].Value;
            }
        }

        /// <summary>
        /// Очищает промежуточную область DC:DQ.
        /// </summary>
        private static void ClearTempArea(ExcelWorksheet ws, int lastRow)
        {
            for (int r = TempStartRow; r <= lastRow; r++)
            {
                for (int c = ColDC; c <= ColDQ; c++)
                {
                    ws.Cells[r, c].Value = null;
                }
            }
        }

        /// <summary>
        /// Находит последнюю строку в указанном столбце листа Excel, содержащую непустую ячейку, начиная с заданной строки.
        /// </summary>
        /// <remarks>Ячейки, содержащие только пробелы, считаются пустыми и игнорируются. Метод  ищет начиная с указанной строки до последней строки с данными на листе.</remarks>
        /// <param name="ws">Лист Excel для поиска данных</param>
        /// <param name="col">Индекс столбца (с единицы), в котором проверяются непустые ячейки</param>
        /// <param name="fromRow">Индекс строки (с единицы), с которой начинается поиск</param>
        /// <returns>Индекс последней строки (с единицы) в указанном столбце, содержащей непустую ячейку. Возвращает индекс начальной строки, если непустых ячеек не найдено</returns>
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
        /// TODO. НЕПРАВИЛЬНО! Устанавливает количество строк в указанном листе Excel в заранее определённой ячейке.
        /// </summary>
        /// <param name="ws">Экземпляр ExcelWorksheet, в котором будет установлено количество строк. Не может быть null.</param>
        /// <param name="value">Количество строк, которое нужно установить на рабочем листе. Должно быть неотрицательным целым числом.</param>

        private static void SetRowCount(ExcelWorksheet ws, int value)
        {
            ws.Cells[19, 9].Value = value; // I19
        }
    }
}
