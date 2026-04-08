using System;
using System.Collections.Generic;
using System.Linq;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Wpf.ViewModels;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Mx (крутящий момент) в нотации IDEA StatiCA.
    /// Mx/MK в Лира > Mx в IDEA StatiCA.
    /// MAX Mx > MAX T.
    /// </summary>
    public class IdeaMxTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: MAX Mx маппится в MAX T, значение Mx передаётся без изменений.
        /// </summary>
        [Fact]
        public void MaxMx_MappedTo_MaxT()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.2", mx: "4.44", my: "0.3", mz: "0.4", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMx = analysisRows.First(r => r.RowType == "MAX Mx");

            Assert.Equal("MAX T", ViewModel.MapRowTypeToIdea(maxMx.RowType));
            Assert.Equal(4.44, maxMx.Mx);  // > Mx в IDEA
        }

        /// <summary>
        /// Проверяет: при нескольких строках MAX T (Mx) выбирает строку с максимальным |Mx|.
        /// </summary>
        [Fact]
        public void MaxT_SelectsMaxAbsMx()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.1", mx: "1.1", my: "0", mz: "0", mw: "0"),
                Make(dcl: "2", n: "1", qy: "0.1", qz: "0.1", mx: "-7.7", my: "0", mz: "0", mw: "0"),
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMx = analysisRows.First(r => r.RowType == "MAX Mx");

            Assert.Equal(-7.7, maxMx.Mx);  // > Mx = -7.7
        }

        /// <summary>
        /// Проверяет: отрицательное значение Mx сохраняет знак.
        /// </summary>
        [Fact]
        public void Mx_PreservesSign()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.1", mx: "-2.22", my: "0", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMx = analysisRows.First(r => r.RowType == "MAX Mx");

            Assert.Equal(-2.22, maxMx.Mx);
        }
    }
}
