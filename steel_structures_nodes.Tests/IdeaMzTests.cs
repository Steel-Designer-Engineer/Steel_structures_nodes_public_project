using System;
using System.Collections.Generic;
using System.Linq;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Wpf.ViewModels;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Mz (изгибающий момент) в нотации IDEA StatiCA.
    /// MZ в Лира > Mz в IDEA StatiCA.
    /// MAX Mz > MAX Mo.
    /// </summary>
    public class IdeaMzTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: MAX Mz маппится в MAX Mo, значение Mz передаётся без изменений.
        /// </summary>
        [Fact]
        public void MaxMz_MappedTo_MaxMo()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.2", mx: "0.3", my: "0.4", mz: "6.66", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMz = analysisRows.First(r => r.RowType == "MAX Mz");

            Assert.Equal("MAX Mo", ViewModel.MapRowTypeToIdea(maxMz.RowType));
            Assert.Equal(6.66, maxMz.Mz);  // > Mz в IDEA
        }

        /// <summary>
        /// Проверяет: при нескольких строках MAX Mo (Mz) выбирает строку с максимальным |Mz|.
        /// </summary>
        [Fact]
        public void MaxMo_SelectsMaxAbsMz()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.1", mx: "0", my: "0", mz: "1.5", mw: "0"),
                Make(dcl: "2", n: "1", qy: "0.1", qz: "0.1", mx: "0", my: "0", mz: "-9.3", mw: "0"),
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMz = analysisRows.First(r => r.RowType == "MAX Mz");

            Assert.Equal(-9.3, maxMz.Mz);  // > Mz = -9.3
        }

        /// <summary>
        /// Проверяет: отрицательное значение Mz сохраняет знак.
        /// </summary>
        [Fact]
        public void Mz_PreservesSign()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.1", mx: "0", my: "0", mz: "-3.33", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMz = analysisRows.First(r => r.RowType == "MAX Mz");

            Assert.Equal(-3.33, maxMz.Mz);
        }
    }
}
