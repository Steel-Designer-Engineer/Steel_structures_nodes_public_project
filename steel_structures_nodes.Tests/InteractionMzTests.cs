using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Mz — изгибающий момент Mz из interaction_tables.json.
    /// </summary>
    public class InteractionMzTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Mz_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Mz");
                Assert.Equal(expected, e.Node.Mz);
            }
        }
    }
}
