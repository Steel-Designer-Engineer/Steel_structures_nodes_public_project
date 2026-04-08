using System;
using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Qz — поперечная сила (MAX Qz) из проекта steel_structures_nodes.Calculate.
    /// <see cref="Calculator.BuildAnalysisTable"/> ищет строку с максимальным |Qz|
    /// и возвращает все усилия этой строки целиком (одна комбинация нагрузок).
    /// Qz входит в числитель формулы коэффициента использования:
    /// <c>U = |Qz| / AlbumQy + |N| / AlbumNt</c>,
    /// где AlbumQy — несущая способность по поперечной силе из альбома.
    /// Чем больше |Qz|, тем выше U.
    /// </summary>
    public class QzTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет выбор строки с максимальным |Qz|.
        /// Входные данные: Qz=3, Qz=-7, Qz=4. Максимум |Qz|=7 у dcl="2".
        /// </summary>
        [Fact]
        public void SelectsRowWithMaxAbsQz()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", qz: "3"),
                Make(dcl: "2", qz: "-7"),
                Make(dcl: "3", qz: "4"),
            };
            var row = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>())[4];
            Assert.Equal("MAX Qz", row.RowType);
            Assert.Equal(-7d, row.Qz);
            Assert.Equal("2", row.LoadCombination);
        }

        /// <summary>
        /// Проверяет: Qz=-6 > SummaryQz=6 (модуль).
        /// </summary>
        [Fact]
        public void SummaryQz_TakesAbsValue()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Qz", Qz = -6 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(6d, summary.SummaryQz);
        }

        /// <summary>
        /// Проверяет обнуление: Qz=0 > SummaryQz=null.
        /// </summary>
        [Fact]
        public void SummaryQz_ZeroBecomes_Null()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Qz", Qz = 0 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Null(summary.SummaryQz);
        }
    }
}
