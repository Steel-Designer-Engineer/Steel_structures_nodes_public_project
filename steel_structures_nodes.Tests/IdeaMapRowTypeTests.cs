using System;
using System.Collections.Generic;
using System.Linq;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Wpf.ViewModels;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты маппинга наименований строк анализа > IDEA StatiCA
    /// (<see cref="ViewModel.MapRowTypeToIdea"/>).
    /// </summary>
    public class IdeaMapRowTypeTests : CalculatorTestBase
    {
        [Theory]
        [InlineData("MAX Qy", "MAX Qo")]
        [InlineData("MAX Qz", "MAX Q")]
        [InlineData("MAX Mx", "MAX T")]
        [InlineData("MAX My", "MAX M")]
        [InlineData("MAX Mz", "MAX Mo")]
        [InlineData("MAX N+", "MAX Nt")]
        [InlineData("MAX N-", "MAX Nc")]
        public void MapsKnownTypes(string input, string expected)
        {
            Assert.Equal(expected, ViewModel.MapRowTypeToIdea(input));
        }

        [Theory]
        [InlineData("MAX Coeff")]
        [InlineData("MAX u")]
        [InlineData("MAX N")]
        [InlineData("MAX Mw")]
        public void PreservesUnchangedTypes(string input)
        {
            Assert.Equal(input, ViewModel.MapRowTypeToIdea(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void HandlesNullAndEmpty(string input)
        {
            Assert.Equal(input, ViewModel.MapRowTypeToIdea(input));
        }

        /// <summary>
        /// Проверяет: все 11 строк анализа имеют валидные IDEA-наименования.
        /// </summary>
        [Fact]
        public void AllAnalysisRows_HaveValidIdeaNames()
        {
            var rsu = new List<ForceRow>
            {
                Make(n: "10", qy: "2", qz: "3", mx: "0.5", my: "4", mz: "1", mw: "0.01")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());

            var expectedNames = new HashSet<string>
            {
                "MAX N", "MAX Nt", "MAX Nc",
                "MAX Qo", "MAX Q",
                "MAX T", "MAX M", "MAX Mo",
                "MAX Mw", "MAX Coeff", "MAX u"
            };

            foreach (var row in analysisRows)
            {
                var ideaName = ViewModel.MapRowTypeToIdea(row.RowType);
                Assert.False(string.IsNullOrWhiteSpace(ideaName), $"IDEA name for '{row.RowType}' is empty");
                Assert.Contains(ideaName, expectedNames);
            }
        }

        /// <summary>
        /// Проверяет: количество строк IDEA совпадает с количеством строк анализа (11).
        /// </summary>
        [Fact]
        public void IdeaRows_CountMatchesAnalysisRows()
        {
            var rsu = new List<ForceRow>
            {
                Make(n: "5", qy: "1", qz: "2", mx: "0.5", my: "3", mz: "1.5", mw: "0.1")
            };

            var analysisRows = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            Assert.Equal(11, analysisRows.Count);
        }
    }
}
