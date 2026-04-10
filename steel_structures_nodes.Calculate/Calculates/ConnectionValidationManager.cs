using System;
using System.Collections.Generic;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.DataValidation.Contracts;

namespace steel_structures_nodes.Calculate.Calculate
{
    /// <summary>
    /// Настройка выпадающих списков (Data Validation) на расчётном листе
    /// в зависимости от типа узла соединения.
    /// Эквивалент VBA-макросов H1()..BSH() — установка валидации на ячейки A3 и F3.
    ///
    /// Логика:
    ///   • Все типы → ячейка A3 получает выпадающий список из именованного диапазона «_ТипУзла»
    ///   • Некоторые типы (H13, H14, R1–R4) → ячейка F3 получает дополнительный
    ///     выпадающий список для выбора варианта исполнения
    /// </summary>
    public class ConnectionValidationManager
    {
        /// <summary>Ячейка типа узла.</summary>
        private const int CellA3Row = 3;
        private const int CellA3Col = 1;

        /// <summary>Ячейка варианта исполнения.</summary>
        private const int CellF3Row = 3;
        private const int CellF3Col = 6;

        /// <summary>
        /// Определение валидации: именованный диапазон для A3 и (опционально) для F3.
        /// </summary>
        private class ValidationDef
        {
            public string A3Formula { get; init; }
            public string F3Formula { get; init; }
        }

        /// <summary>
        /// Маппинг: тип узла → формулы именованных диапазонов для валидации.
        /// Извлечено из VBA-макросов H1()..BSH().
        /// </summary>
        private static readonly Dictionary<string, ValidationDef> ValidationMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // H-серия
                ["H1"]   = new() { A3Formula = "=_H1" },
                ["H1H"]  = new() { A3Formula = "=_H1H" },
                ["H2"]   = new() { A3Formula = "=_H2" },
                ["H2_"]  = new() { A3Formula = "=_H2_" },
                ["H2H"]  = new() { A3Formula = "=_H2H" },
                ["H2H_"] = new() { A3Formula = "=_H2H_" },
                ["H2SH"] = new() { A3Formula = "=_H2SH" },
                ["H3"]   = new() { A3Formula = "=_H3" },
                ["H3H"]  = new() { A3Formula = "=_H3H" },
                ["H4"]   = new() { A3Formula = "=_H4" },
                ["H4H"]  = new() { A3Formula = "=_H4H" },
                ["H5"]   = new() { A3Formula = "=_H5" },
                ["H5H"]  = new() { A3Formula = "=_H5H" },
                ["H6"]   = new() { A3Formula = "=_H6" },
                ["H6H"]  = new() { A3Formula = "=_H6H" },
                ["H7"]   = new() { A3Formula = "=_H7" },
                ["H7H"]  = new() { A3Formula = "=_H7H" },
                ["H8"]   = new() { A3Formula = "=_H8" },
                ["H9"]   = new() { A3Formula = "=_H9" },
                ["H10"]  = new() { A3Formula = "=_H10" },
                ["H11"]  = new() { A3Formula = "=_H11" },
                ["H12"]  = new() { A3Formula = "=_H12" },
                ["H13"]  = new() { A3Formula = "=_H13",  F3Formula = "=H13_Var" },
                ["H14"]  = new() { A3Formula = "=_H14",  F3Formula = "=H14_Var" },

                // S-серия
                ["S1"]   = new() { A3Formula = "=_S1" },
                ["S1H"]  = new() { A3Formula = "=_S1H" },
                ["S2"]   = new() { A3Formula = "=_S2" },
                ["S2H"]  = new() { A3Formula = "=_S2H" },
                ["S3"]   = new() { A3Formula = "=_S3" },
                ["S4"]   = new() { A3Formula = "=_S4" },

                // P-серия
                ["P1"]   = new() { A3Formula = "=_P1" },
                ["P2"]   = new() { A3Formula = "=_P2" },
                ["P3"]   = new() { A3Formula = "=_P3" },
                ["P4"]   = new() { A3Formula = "=_P4" },
                ["P5"]   = new() { A3Formula = "=_P5" },
                ["P6"]   = new() { A3Formula = "=_P6" },

                // R-серия (с дополнительным вариантом на F3)
                ["R1"]   = new() { A3Formula = "=_R1",   F3Formula = "=_R1_R2_Var" },
                ["R2"]   = new() { A3Formula = "=_R2",   F3Formula = "=_R1_R2_Var" },
                ["R3"]   = new() { A3Formula = "=_R3",   F3Formula = "=_R3_R4_Var" },
                ["R4"]   = new() { A3Formula = "=_R4",   F3Formula = "=_R3_R4_Var" },

                // B-серия
                ["B"]    = new() { A3Formula = "=_B" },
                ["BH"]   = new() { A3Formula = "=_BH" },
                ["BSH"]  = new() { A3Formula = "=_BSH" },
            };

        /// <summary>
        /// Устанавливает валидацию (выпадающие списки) на ячейки A3 и F3
        /// в соответствии с типом узла.
        /// </summary>
        /// <param name="ws">Рабочий лист.</param>
        /// <param name="connectionType">Тип узла (значение ячейки C3).</param>
        /// <returns><c>true</c>, если валидация установлена; <c>false</c>, если тип не найден.</returns>
        public bool ApplyValidation(ExcelWorksheet ws, string connectionType)
        {
            if (ws == null) throw new ArgumentNullException(nameof(ws));

            if (string.IsNullOrWhiteSpace(connectionType))
                return false;

            if (!ValidationMap.TryGetValue(connectionType.Trim(), out var def))
                return false;

            // Установить валидацию на A3
            SetListValidation(ws, CellA3Row, CellA3Col, def.A3Formula);

            // Установить валидацию на F3 (если есть вариант исполнения)
            if (!string.IsNullOrEmpty(def.F3Formula))
            {
                SetListValidation(ws, CellF3Row, CellF3Col, def.F3Formula);
            }

            return true;
        }

        /// <summary>
        /// Проверяет, имеет ли тип узла дополнительный вариант исполнения (F3).
        /// </summary>
        public static bool HasVariant(string connectionType)
        {
            if (string.IsNullOrWhiteSpace(connectionType))
                return false;

            return ValidationMap.TryGetValue(connectionType.Trim(), out var def)
                   && !string.IsNullOrEmpty(def.F3Formula);
        }

        /// <summary>
        /// Возвращает формулу именованного диапазона для валидации A3, или <c>null</c>.
        /// </summary>
        public static string GetA3Formula(string connectionType)
        {
            if (string.IsNullOrWhiteSpace(connectionType))
                return null;

            return ValidationMap.TryGetValue(connectionType.Trim(), out var def)
                ? def.A3Formula
                : null;
        }

        /// <summary>
        /// Возвращает формулу именованного диапазона для валидации F3, или <c>null</c>.
        /// </summary>
        public static string GetF3Formula(string connectionType)
        {
            if (string.IsNullOrWhiteSpace(connectionType))
                return null;

            return ValidationMap.TryGetValue(connectionType.Trim(), out var def)
                ? def.F3Formula
                : null;
        }

        /// <summary>
        /// Возвращает все поддерживаемые типы соединений.
        /// </summary>
        public static IReadOnlyCollection<string> GetSupportedTypes()
        {
            return ValidationMap.Keys;
        }

        /// <summary>
        /// Устанавливает Data Validation типа List на указанную ячейку.
        /// Удаляет предыдущую валидацию, затем создаёт новую.
        /// </summary>
        private static void SetListValidation(ExcelWorksheet ws, int row, int col, string formula)
        {
            var cellAddress = ws.Cells[row, col].Address;

            // Удалить существующую валидацию на ячейке (если есть)
            RemoveExistingValidation(ws, cellAddress);

            // Создать новую валидацию
            var validation = ws.DataValidations.AddListValidation(cellAddress);
            validation.ShowErrorMessage = true;
            validation.ShowInputMessage = true;
            validation.AllowBlank = true;
            validation.Formula.ExcelFormula = formula;
        }

        /// <summary>
        /// Удаляет существующую валидацию для указанного адреса ячейки.
        /// </summary>
        private static void RemoveExistingValidation(ExcelWorksheet ws, string address)
        {
            var toRemove = new List<IExcelDataValidation>();
            foreach (var dv in ws.DataValidations)
            {
                if (string.Equals(dv.Address.Address, address, StringComparison.OrdinalIgnoreCase))
                    toRemove.Add(dv);
            }

            foreach (var dv in toRemove)
            {
                ws.DataValidations.Remove(dv);
            }
        }
    }
}
