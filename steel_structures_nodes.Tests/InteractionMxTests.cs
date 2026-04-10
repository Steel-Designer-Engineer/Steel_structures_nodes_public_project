using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной Mx — крутящий момент из interaction_tables.json.
    /// </summary>
    public class InteractionMxTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Mx_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Mx");
                Assert.Equal(expected, e.Node.Mx);
            }
        }
    }
}
