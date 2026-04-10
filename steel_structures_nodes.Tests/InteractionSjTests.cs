using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Sj — начальная жёсткость из interaction_tables.json.
    /// </summary>
    public class InteractionSjTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Sj_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Sj");
                Assert.Equal(expected, e.Node.Sj);
            }
        }
    }
}
