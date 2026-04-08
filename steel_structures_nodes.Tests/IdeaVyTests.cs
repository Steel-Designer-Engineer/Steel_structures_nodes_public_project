using System;
using System.Collections.Generic;
using System.Linq;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Wpf.ViewModels;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Vy (поперечная сила) в нотации IDEA StatiCA.
    /// Qy в Лира > Vy в IDEA StatiCA.
    /// MAX Qy > MAX Qo.
    /// </summary>
    public class IdeaVyTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: MAX Qy маппится в MAX Qo, значение Qy попадает в Vy.
        /// </summary>
        [Fact]
        public void MaxQy_MappedTo_MaxQo()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "7.77", qz: "0.5", mx: "0.1", my: "0.2", mz: "0.3", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxQy = analysisRows.First(r => r.RowType == "MAX Qy");

            Assert.Equal("MAX Qo", ViewModel.MapRowTypeToIdea(maxQy.RowType));
            Assert.Equal(7.77, maxQy.Qy);  // > Vy в IDEA
        }

        /// <summary>
        /// Проверяет: при нескольких строках MAX Qo (Vy) выбирает строку с максимальным |Qy|.
        /// </summary>
        [Fact]
        public void MaxQo_SelectsMaxAbsQy()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "2.0", qz: "0.1", mx: "0", my: "0", mz: "0", mw: "0"),
                Make(dcl: "2", n: "1", qy: "-5.5", qz: "0.1", mx: "0", my: "0", mz: "0", mw: "0"),
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxQy = analysisRows.First(r => r.RowType == "MAX Qy");

            Assert.Equal(-5.5, maxQy.Qy);  // > Vy = -5.5
        }

        /// <summary>
        /// Проверяет: отрицательное значение Qy сохраняет знак при маппинге в Vy.
        /// </summary>
        [Fact]
        public void Vy_PreservesSign()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "-3.14", qz: "0.1", mx: "0", my: "0", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxQy = analysisRows.First(r => r.RowType == "MAX Qy");

            Assert.Equal(-3.14, maxQy.Qy);
        }
    }
}
