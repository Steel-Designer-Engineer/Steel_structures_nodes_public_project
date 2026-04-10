using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты полей ProfileBeam и ProfileColumn из interaction_tables.json.
    /// </summary>
    public class InteractionProfileTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_ProfileBeam_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = Parser.TryGetString(e.Raw, "ProfileBeam");
                Assert.Equal(expected, e.Node.ProfileBeam);
            }
        }

        [Fact]
        public void AllEntries_ProfileColumn_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = Parser.TryGetString(e.Raw, "ProfileColumn");
                Assert.Equal(expected, e.Node.ProfileColumn);
            }
        }
    }
}
