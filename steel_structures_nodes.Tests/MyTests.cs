using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной My — изгибающий момент (MAX My) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным |My| (MY из SCAD)
    /// и возвращает все усилия этой строки целиком.
    /// </summary>
    public class MyTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным |My|.
        /// Входные данные: My=6, My=-11, My=3. Максимум |My|=11 у dcl="2".
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxAbsMy()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", my: "6"),
                Make(dcl: "2", my: "-11"),
                Make(dcl: "3", my: "3"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[6];
            Assert.Equal("MAX My", row.RowType);
            Assert.Equal(-11d, row.My);
            Assert.Equal("2", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет: My=-12 > SummaryMy=12 (модуль).
        /// </summary>
        [Fact]
        public void SummaryMy_TakesAbsValue()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX My", My = -12 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(12d, summary.SummaryMy);
        }

        /// <summary>
        /// Проверяет обнуление: My=0 > SummaryMy=null.
        /// </summary>
        [Fact]
        public void SummaryMy_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX My", My = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryMy);
        }
    }
}
