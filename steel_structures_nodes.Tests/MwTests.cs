using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Mw — бимомент (MAX Mw) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным |Mw| (MW из SCAD)
    /// и возвращает все усилия этой строки целиком.
    /// Бимомент возникает при стеснённом кручении тонкостенных стержней.
    /// </summary>
    public class MwTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным |Mw|.
        /// Входные данные: Mw=0.3, Mw=-0.8, Mw=0.5. Максимум |Mw|=0.8 у dcl="2".
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxAbsMw()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", mw: "0.3"),
                Make(dcl: "2", mw: "-0.8"),
                Make(dcl: "3", mw: "0.5"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[8];
            Assert.Equal("MAX Mw", row.RowType);
            Assert.Equal(-0.8d, row.Mw);
            Assert.Equal("2", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет: Mw=-2.2 > SummaryMw=2.2 (модуль).
        /// </summary>
        [Fact]
        public void SummaryMw_TakesAbsValue()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Mw", Mw = -2.2 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(2.2, summary.SummaryMw);
        }

        /// <summary>
        /// Проверяет обнуление: Mw=0 > SummaryMw=null.
        /// </summary>
        [Fact]
        public void SummaryMw_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Mw", Mw = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryMw);
        }
    }
}
