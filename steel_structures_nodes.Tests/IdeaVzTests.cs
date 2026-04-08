using System;
using System.Collections.Generic;
using System.Linq;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Wpf.ViewModels;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Vz (поперечная сила) в нотации IDEA StatiCA.
    /// Qz в Лира > Vz в IDEA StatiCA.
    /// MAX Qz > MAX Q.
    /// </summary>
    public class IdeaVzTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: MAX Qz маппится в MAX Q, значение Qz попадает в Vz.
        /// </summary>
        [Fact]
        public void MaxQz_MappedTo_MaxQ()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.5", qz: "9.99", mx: "0.1", my: "0.2", mz: "0.3", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxQz = analysisRows.First(r => r.RowType == "MAX Qz");

            Assert.Equal("MAX Q", ViewModel.MapRowTypeToIdea(maxQz.RowType));
            Assert.Equal(9.99, maxQz.Qz);  // > Vz в IDEA
        }

        /// <summary>
        /// Проверяет: при нескольких строках MAX Q (Vz) выбирает строку с максимальным |Qz|.
        /// </summary>
        [Fact]
        public void MaxQ_SelectsMaxAbsQz()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "1.5", mx: "0", my: "0", mz: "0", mw: "0"),
                Make(dcl: "2", n: "1", qy: "0.1", qz: "-8.2", mx: "0", my: "0", mz: "0", mw: "0"),
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxQz = analysisRows.First(r => r.RowType == "MAX Qz");

            Assert.Equal(-8.2, maxQz.Qz);  // > Vz = -8.2
        }

        /// <summary>
        /// Проверяет: отрицательное значение Qz сохраняет знак при маппинге в Vz.
        /// </summary>
        [Fact]
        public void Vz_PreservesSign()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "-4.56", mx: "0", my: "0", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxQz = analysisRows.First(r => r.RowType == "MAX Qz");

            Assert.Equal(-4.56, maxQz.Qz);
        }
    }
}
