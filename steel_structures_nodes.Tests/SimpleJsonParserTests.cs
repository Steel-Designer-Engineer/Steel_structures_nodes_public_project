using System;
using Steel_structures_nodes_public_project.Wpf.Services;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    public class SimpleJsonParserTests
    {
        [Fact]
        public void TryReadConnectionCodesFromArray_ReturnsCodes()
        {
            var json = @"[
                { ""CONNECTION_CODE"": ""H1_01"", ""ProfileBeam"": ""IPE"", ""ProfileColumn"": ""HEA"" },
                { ""CONNECTION_CODE"": ""H1_02"", ""ProfileBeam"": ""IPE"", ""ProfileColumn"": ""HEA"" }
            ]";

            var p = new SimpleJsonParser(json);
            var codes = p.TryReadConnectionCodesFromArray();
            Assert.NotNull(codes);
            Assert.Contains("H1_01", codes);
            Assert.Contains("H1_02", codes);
        }

        [Fact]
        public void TryFindInteractionConnectionName_MatchesByNameAndBeam()
        {
            var json = @"[
                { ""Name"": ""H12"", ""CONNECTION_CODE"": ""H12_01"", ""ProfileBeam"": ""IPE"", ""ProfileColumn"": ""HEA"" },
                { ""Name"": ""H12"", ""CONNECTION_CODE"": ""H12_02"", ""ProfileBeam"": ""HEA"", ""ProfileColumn"": ""IPE"" }
            ]";

            var p = new SimpleJsonParser(json);
            var code = p.TryFindInteractionConnectionName("H12", "IPE");
            Assert.Equal("H12_01", code);

            var code2 = p.TryFindInteractionConnectionName("H12", "IPE", "HEA", "H12_01");
            Assert.Equal("H12_01", code2);
        }
    }
}
