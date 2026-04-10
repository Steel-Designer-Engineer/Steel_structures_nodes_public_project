using System;
using System.Collections.Generic;
using steel_structures_nodes.Calculate.Calculate;
using steel_structures_nodes.Calculate.Models;
using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Qy — поперечная сила (MAX Qy) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным |Qy|
    /// среди всех комбинаций нагрузок и возвращает все усилия этой строки целиком.
    /// В итоговом Summary значение берётся по модулю.
    /// </summary>
    public class QyTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным |Qy|.
        /// Входные данные: Qy=5, Qy=-12, Qy=8.
        /// |Qy| максимален у строки dcl="2" (|-12|=12), она и выбирается.
        /// В результате Qy сохраняет знак (-12), LoadCombination="2".
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxAbsQy()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", qy: "5", n: "1"),
                Make(dcl: "2", qy: "-12", n: "2"),
                Make(dcl: "3", qy: "8", n: "3"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[3];
            Assert.Equal("MAX Qy", row.RowType);
            Assert.Equal(-12d, row.Qy);
            Assert.Equal("2", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет ExtractSummary: отрицательное значение Qy=-8 преобразуется
        /// в SummaryQy=8 (модуль). Summary хранит абсолютные значения для сравнения с альбомом.
        /// </summary>
        [Fact]
        public void SummaryQy_TakesAbsValue()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Qy", Qy = -8 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(8d, summary.SummaryQy);
        }

        /// <summary>
        /// Проверяет обнуление: Qy=0 > SummaryQy=null.
        /// Нулевая поперечная сила не считается значимым результатом.
        /// </summary>
        [Fact]
        public void SummaryQy_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Qy", Qy = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryQy);
        }
    }
}
