using System.Globalization;
using Steel_structures_nodes_public_project.Calculate.Models;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    public class ForceRowTests
    {
        [Theory]
        [InlineData("1.23", 1.23)]
        [InlineData("1,23", 1.23)]
        [InlineData("-2,5", -2.5)]
        public void ParsedN_ParsesNumbers(string text, double expected)
        {
            var r = new ForceRow { N = text };
            Assert.Equal(expected, r.ParsedN.Value, 5);
        }

        [Fact]
        public void ParsedN_NullOrEmpty_ReturnsNull()
        {
            var r1 = new ForceRow { N = null };
            var r2 = new ForceRow { N = "" };
            Assert.Null(r1.ParsedN);
            Assert.Null(r2.ParsedN);
        }

        [Fact]
        public void ParsedFields_MultipleColumns()
        {
            var r = new ForceRow
            {
                N = "1,5", 
                Qy = "4",
                Qz = "",
                Mx = "2.5", 
                My = "-3,25", 
                Mz = "", 
            };
            Assert.Equal(1.5, r.ParsedN.Value, 5);
            Assert.Equal(2.5, r.ParsedMx.Value, 5);
            Assert.Equal(-3.25, r.ParsedMy.Value, 5);
            Assert.Null(r.ParsedMz);
            Assert.Equal(4d, r.ParsedQy.Value, 5);
        }
    }
}
