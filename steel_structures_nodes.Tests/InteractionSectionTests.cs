using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты параметров сечения (H, B, s, t) из interaction_tables.json.
    /// </summary>
    public class InteractionSectionTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_SectionH_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "H");
                Assert.Equal(expected, e.Node.BeamH);
            }
        }

        [Fact]
        public void AllEntries_SectionB_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "B");
                Assert.Equal(expected, e.Node.BeamB);
            }
        }

        [Fact]
        public void AllEntries_SectionS_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "s");
                Assert.Equal(expected, e.Node.BeamS);
            }
        }

        [Fact]
        public void AllEntries_SectionT_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "t");
                Assert.Equal(expected, e.Node.BeamT);
            }
        }
    }
}
