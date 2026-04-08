using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Nc — сжимающая продольная сила (MAX N-) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с минимальным отрицательным N
    /// среди всех комбинаций нагрузок и возвращает все усилия этой строки целиком.
    /// </summary>
    public class NcTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с минимальным (наибольшим по модулю) отрицательным N.
        /// Входные данные: N=10, N=-15, N=-40.
        /// Ожидается: MAX N- выбирает строку с N=-40 (dcl="3").
        /// </summary>
        [Fact]
        public void SelectsRowWithMinNegativeN()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", n: "10"),
                Make(dcl: "2", n: "-15"),
                Make(dcl: "3", n: "-40"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[2];
            Assert.Equal("MAX N-", row.RowType);
            Assert.Equal(-40d, row.Nc);
            Assert.Equal(-40d, row.N);
            Assert.Equal("3", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет граничный случай: все значения N положительные.
        /// Отрицательного N нет > MAX N- возвращает «пустую» строку (N=0).
        /// </summary>
        [Fact]
        public void AllPositive_ReturnsEmptyRow()
        {
            var rows = new List<ForceRow> { Make(n: "10"), Make(n: "20") };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[2];
            Assert.Equal("MAX N-", row.RowType);
            Assert.Equal(0d, row.N);
        }

        /// <summary>
        /// Проверяет ExtractSummary: из строки "MAX N-" с Nc=-18
        /// формируется SummaryNc=-18 (отрицательное значение сохраняется).
        /// </summary>
        [Fact]
        public void SummaryNc_EqualsMinNegativeN()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX N-", Nc = -18 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(-18d, summary.SummaryNc);
        }

        /// <summary>
        /// Проверяет обнуление: Nc=0 > SummaryNc=null.
        /// Нулевое сжатие не считается значимым результатом.
        /// </summary>
        [Fact]
        public void SummaryNc_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX N-", Nc = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryNc);
        }
    }
}
