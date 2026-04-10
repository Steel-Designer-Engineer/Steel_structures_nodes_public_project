using System;
using System.Collections.Generic;
using steel_structures_nodes.Calculate.Calculate;
using steel_structures_nodes.Calculate.Models;
using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Mx — крутящий момент (MAX Mx) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным |Mx| (MX из SCAD)
    /// и возвращает все усилия этой строки целиком.
    /// </summary>
    public class MxTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным |Mx|.
        /// Входные данные: Mx=2, Mx=-9, Mx=5. Максимум |Mx|=9 у dcl="2".
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxAbsMx()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", mx: "2"),
                Make(dcl: "2", mx: "-9"),
                Make(dcl: "3", mx: "5"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[5];
            Assert.Equal("MAX Mx", row.RowType);
            Assert.Equal(-9d, row.Mx);
            Assert.Equal("2", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет: Mx=-3 > SummaryMx=3 (модуль для сравнения с альбомом).
        /// </summary>
        [Fact]
        public void SummaryMx_TakesAbsValue()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Mx", Mx = -3 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(3d, summary.SummaryMx);
        }

        /// <summary>
        /// Проверяет обнуление: Mx=0 > SummaryMx=null.
        /// </summary>
        [Fact]
        public void SummaryMx_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Mx", Mx = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryMx);
        }
    }
}
