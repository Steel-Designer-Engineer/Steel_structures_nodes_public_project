using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Mw — бимомент из interaction_tables.json.
    /// </summary>
    public class InteractionMwTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Mw_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Mw");
                Assert.Equal(expected, e.Node.Mw);
            }
        }
    }
}
