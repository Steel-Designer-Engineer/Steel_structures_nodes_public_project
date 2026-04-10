using System;
using System.Collections.Generic;
using System.Linq;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Wpf.ViewModels;
using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной N (продольная сила) в нотации IDEA StatiCA.
    /// N в Лира > N в IDEA StatiCA (без изменений).
    /// MAX N+ > MAX Nt, MAX N- > MAX Nc.
    /// </summary>
    public class IdeaNTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: MAX N сохраняет имя MAX N в IDEA, значение N передаётся без изменений.
        /// </summary>
        [Fact]
        public void MaxN_RowType_PreservedAsMaxN()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "15.5", qy: "0.1", qz: "0.2", mx: "0", my: "0", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxN = analysisRows.First(r => r.RowType == "MAX N");

            Assert.Equal("MAX N", ViewModel.MapRowTypeToIdea(maxN.RowType));
            Assert.Equal(15.5, maxN.N);
        }

        /// <summary>
        /// Проверяет: MAX N+ маппится в MAX Nt в IDEA.
        /// </summary>
        [Fact]
        public void MaxNPlus_MappedTo_MaxNt()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "20", qy: "0.1", qz: "0.2", mx: "0", my: "0", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxNPlus = analysisRows.First(r => r.RowType == "MAX N+");

            Assert.Equal("MAX Nt", ViewModel.MapRowTypeToIdea(maxNPlus.RowType));
            Assert.Equal(20.0, maxNPlus.N);
        }

        /// <summary>
        /// Проверяет: MAX N- маппится в MAX Nc в IDEA.
        /// </summary>
        [Fact]
        public void MaxNMinus_MappedTo_MaxNc()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "-18.3", qy: "0.1", qz: "0.2", mx: "0", my: "0", mz: "0", mw: "0")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxNMinus = analysisRows.First(r => r.RowType == "MAX N-");

            Assert.Equal("MAX Nc", ViewModel.MapRowTypeToIdea(maxNMinus.RowType));
            Assert.Equal(-18.3, maxNMinus.N);
        }

        /// <summary>
        /// Проверяет: при нескольких строках MAX N выбирает строку с максимальным |N|.
        /// </summary>
        [Fact]
        public void MaxN_SelectsRowWithMaxAbsN()
        {
            var rsu = new List<ForceRow>
            {
                Make(dcl: "1", n: "3", qy: "0.1", qz: "0.1", mx: "0", my: "0", mz: "0", mw: "0"),
                Make(dcl: "2", n: "-25", qy: "0.1", qz: "0.1", mx: "0", my: "0", mz: "0", mw: "0"),
                Make(dcl: "3", n: "10", qy: "0.1", qz: "0.1", mx: "0", my: "0", mz: "0", mw: "0"),
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            var maxN = analysisRows.First(r => r.RowType == "MAX N");

            Assert.Equal(-25.0, maxN.N);
        }
    }
}
