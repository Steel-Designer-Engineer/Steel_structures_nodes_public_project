using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Qz — поперечная сила Qz из interaction_tables.json.
    /// </summary>
    public class InteractionQzTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Qz_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Qz");
                Assert.Equal(expected, e.Node.Qz);
            }
        }
    }
}
