using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Nc — сжатие из interaction_tables.json.
    /// </summary>
    public class InteractionNcTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Nc_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Nc");
                Assert.Equal(expected, e.Node.Nc);
            }
        }
    }
}
