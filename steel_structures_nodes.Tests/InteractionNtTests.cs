using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Nt — растяжение из interaction_tables.json.
    /// </summary>
    public class InteractionNtTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Nt_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Nt");
                Assert.Equal(expected, e.Node.Nt);
            }
        }
    }
}
