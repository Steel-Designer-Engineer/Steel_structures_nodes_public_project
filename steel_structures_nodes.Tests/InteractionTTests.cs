using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной T — крутящий момент (T) из interaction_tables.json.
    /// </summary>
    public class InteractionTTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_T_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "T");
                Assert.Equal(expected, e.Node.T);
            }
        }
    }
}
