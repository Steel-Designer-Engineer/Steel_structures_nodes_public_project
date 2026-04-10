using System;
using System.Collections.Generic;
using System.Linq;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Wpf.ViewModels;
using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной My (изгибающий момент) в нотации IDEA StatiCA.
    /// MY в Лира > My в IDEA StatiCA.
    /// MAX My > MAX M.
    /// </summary>
    public class IdeaMyTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: MAX My маппится в MAX M, значение My передаётся без изменений.
        /// </summary>
        [Fact]
        public void MaxMy_MappedTo_MaxM()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.2", mx: "0.3", my: "8.88", mz: "0.4", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMy = analysisRows.First(r => r.RowType == "MAX My");

            Assert.Equal("MAX M", ViewModel.MapRowTypeToIdea(maxMy.RowType));
            Assert.Equal(8.88, maxMy.My);  // > My в IDEA
        }

        /// <summary>
        /// Проверяет: при нескольких строках MAX M (My) выбирает строку с максимальным |My|.
        /// </summary>
        [Fact]
        public void MaxM_SelectsMaxAbsMy()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.1", mx: "0", my: "2.0", mz: "0", mw: "0"),
                Make(dcl: "2", n: "1", qy: "0.1", qz: "0.1", mx: "0", my: "-11.1", mz: "0", mw: "0"),
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMy = analysisRows.First(r => r.RowType == "MAX My");

            Assert.Equal(-11.1, maxMy.My);  // > My = -11.1
        }

        /// <summary>
        /// Проверяет: отрицательное значение My сохраняет знак.
        /// </summary>
        [Fact]
        public void My_PreservesSign()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "1", qy: "0.1", qz: "0.1", mx: "0", my: "-6.66", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxMy = analysisRows.First(r => r.RowType == "MAX My");

            Assert.Equal(-6.66, maxMy.My);
        }
    }
}
