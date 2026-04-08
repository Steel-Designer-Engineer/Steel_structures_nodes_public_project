using Xunit;

namespace Steel_structures_nodes_public_project.Tests
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
                Assert.Equal(expected, e.Node.SectionH);
            }
        }

        [Fact]
        public void AllEntries_SectionB_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "B");
                Assert.Equal(expected, e.Node.SectionB);
            }
        }

        [Fact]
        public void AllEntries_SectionS_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "s");
                Assert.Equal(expected, e.Node.SectionS);
            }
        }

        [Fact]
        public void AllEntries_SectionT_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "t");
                Assert.Equal(expected, e.Node.SectionT);
            }
        }
    }
}
