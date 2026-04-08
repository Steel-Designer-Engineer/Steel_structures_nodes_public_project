using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной U — коэффициент использования несущей способности (MAX Coeff / MAX u)
    /// из проекта steel_structures_nodes.Calculate.
    /// Формула: <c>U = |Qz| / AlbumQy + |N| / AlbumNt</c>,
    /// где AlbumQy — несущая способность по поперечной силе, AlbumNt — по продольной силе.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным U среди всех комбинаций
    /// и возвращает все усилия этой строки целиком.
    /// U вычисляется для каждой строки таблицы анализа.
    /// </summary>
    public class UTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет формулу коэффициента: U = |Qz|/AlbumQy + |N|/AlbumNt.
        /// AlbumQy=10, AlbumNt=100.
        /// Строка 3: U = |9|/10 + |5|/100 = 0.9 + 0.05 = 0.95 — максимум.
        /// Строка 1: U = |3|/10 + |50|/100 = 0.3 + 0.5 = 0.8.
        /// Строка 2: U = |7|/10 + |10|/100 = 0.7 + 0.1 = 0.8.
        /// MAX Coeff выбирает строку 3 с U=0.95.
        /// </summary>
        [Fact]
        public void MaxCoeff_Formula_QzDivAlbumQy_Plus_NDivAlbumNt()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", qz: "3", n: "50"),
                Make(dcl: "2", qz: "7", n: "10"),
                Make(dcl: "3", qz: "9", n: "5"),
            };
            var table = CreateCalc(qy: 10, nt: 100).BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            Assert.Equal("MAX Coeff", table[9].RowType);
            Assert.Equal(0.95, table[9].U.Value, 5);
            Assert.Equal("3", table[9].LoadCombination);
        }

        /// <summary>
        /// Проверяет: MAX u (строка [9]) использует ту же формулу и даёт тот же результат,
        /// что и MAX Coeff (строка [8]). Обе строки ищут максимальный U.
        /// </summary>
        [Fact]
        public void MaxU_SameAsMaxCoeff()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", qz: "3", n: "50"),
                Make(dcl: "3", qz: "9", n: "5"),
            };
            var table = CreateCalc(qy: 10, nt: 100).BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            Assert.Equal("MAX u", table[10].RowType);
            Assert.Equal(table[9].U.Value, table[10].U.Value, 5);
        }

        /// <summary>
        /// Проверяет: если AlbumQy=0 и AlbumNt=0 (нет данных альбома),
        /// деление на ноль невозможно > MAX Coeff возвращает пустую строку (N=0).
        /// </summary>
        [Fact]
        public void NoAlbumCapacity_ReturnsEmptyRow()
        {
            var rows = new List<ForceRow> { Make(qz: "5", n: "10") };
            var table = CreateCalc(qy: 0, nt: 0).BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            Assert.Equal("MAX Coeff", table[9].RowType);
            Assert.Equal(0d, table[9].N);
        }

        /// <summary>
        /// Проверяет: U вычисляется для ВСЕХ 10 строк таблицы анализа (не только MAX Coeff/u).
        /// Каждая строка MAX получает свой U для сравнения с несущей способностью.
        /// </summary>
        [Fact]
        public void AllRows_HaveU_Calculated()
        {
            var rows = new List<ForceRow>
            {
                Make(n: "20", qz: "5", qy: "1", mx: "0.5", my: "3", mz: "1", mw: "0.1"),
            };
            var table = CreateCalc(qy: 10, nt: 100).BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            foreach (var r in table)
                Assert.True(r.U.HasValue, $"U не вычислен для {r.RowType}");
        }

        /// <summary>
        /// Проверяет ExtractSummary: MaxU берёт наибольший U из всех строк анализа.
        /// Обе строки имеют U=0.7 > MaxU=0.7.
        /// </summary>
        [Fact]
        public void SummaryMaxU_FromAnalysisRows()
        {
            var analysis = new List<AnalysisRow>
            {
                new AnalysisRow { RowType = "MAX Coeff", U = 0.7 },
                new AnalysisRow { RowType = "MAX u", U = 0.7 },
            };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(0.7d, summary.MaxU);
        }

        /// <summary>
        /// Проверяет обнуление: U=0 > MaxU=null.
        /// Нулевой коэффициент использования означает отсутствие нагрузки.
        /// </summary>
        [Fact]
        public void SummaryMaxU_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Coeff", U = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.MaxU);
        }
    }
}
