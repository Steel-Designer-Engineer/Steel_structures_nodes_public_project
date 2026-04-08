using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Nt — растягивающая продольная сила (MAX N+) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным положительным N
    /// среди всех комбинаций нагрузок и возвращает все усилия этой строки целиком.
    /// </summary>
    public class NtTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным положительным N.
        /// Входные данные: N=10, N=50, N=-30.
        /// Ожидается: MAX N+ выбирает строку с N=50 (dcl="2"),
        /// и все сопутствующие усилия берутся из этой же строки (Qy=2).
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxPositiveN()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", n: "10", qy: "1"),
                Make(dcl: "2", n: "50", qy: "2"),
                Make(dcl: "3", n: "-30", qy: "3"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[1];
            Assert.Equal("MAX N+", row.RowType);
            Assert.Equal(50d, row.Nt);
            Assert.Equal(50d, row.N);
            Assert.Equal("2", row.LoadCombination);
            Assert.Equal(2d, row.Qy);
        }

        /// <summary>
        /// Проверяет граничный случай: все значения N отрицательные.
        /// Положительного N нет > MAX N+ должен вернуть «пустую» строку (N=0).
        /// </summary>
        [Fact]
        public void AllNegative_ReturnsEmptyRow()
        {
            var rows = new List<ForceRow> { Make(n: "-10"), Make(n: "-20") };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[1];
            Assert.Equal("MAX N+", row.RowType);
            Assert.Equal(0d, row.N);
        }

        /// <summary>
        /// Проверяет ExtractSummary: из строки анализа "MAX N+" с Nt=42
        /// формируется SummaryNt=42 в итоговом ForceRow.
        /// </summary>
        [Fact]
        public void SummaryNt_EqualsMaxPositiveN()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX N+", Nt = 42 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(42d, summary.SummaryNt);
        }

        /// <summary>
        /// Проверяет обнуление: если Nt=0, то SummaryNt устанавливается в null.
        /// Нулевое значение означает отсутствие растяжения — в итогах это не показывается.
        /// </summary>
        [Fact]
        public void SummaryNt_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX N+", Nt = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryNt);
        }
    }
}
