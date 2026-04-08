using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Mz — изгибающий момент (MAX Mz) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным |Mz| (MZ из SCAD)
    /// и возвращает все усилия этой строки целиком.
    /// </summary>
    public class MzTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным |Mz|.
        /// Входные данные: Mz=1, Mz=-4, Mz=2. Максимум |Mz|=4 у dcl="2".
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxAbsMz()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", mz: "1"),
                Make(dcl: "2", mz: "-4"),
                Make(dcl: "3", mz: "2"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[7];
            Assert.Equal("MAX Mz", row.RowType);
            Assert.Equal(-4d, row.Mz);
            Assert.Equal("2", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет: Mz=-5 > SummaryMz=5 (модуль).
        /// </summary>
        [Fact]
        public void SummaryMz_TakesAbsValue()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Mz", Mz = -5 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(5d, summary.SummaryMz);
        }

        /// <summary>
        /// Проверяет обнуление: Mz=0 > SummaryMz=null.
        /// </summary>
        [Fact]
        public void SummaryMz_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Mz", Mz = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryMz);
        }
    }
}
