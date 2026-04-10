using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Qy — поперечная сила Qy из interaction_tables.json.
    /// </summary>
    public class InteractionQyTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Qy_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Qy");
                Assert.Equal(expected, e.Node.Qy);
            }
        }
    }
}
