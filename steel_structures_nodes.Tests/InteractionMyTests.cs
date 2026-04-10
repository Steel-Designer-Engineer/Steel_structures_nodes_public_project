using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной My — изгибающий момент My из interaction_tables.json.
    /// </summary>
    public class InteractionMyTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_My_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                double? expected = RawDouble(e.Raw, "My");
                Assert.Equal(expected, e.Node.My);
            }
        }
    }
}
