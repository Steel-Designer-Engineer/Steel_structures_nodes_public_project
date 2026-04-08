using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Mneg — отрицательный момент из interaction_tables.json.
    /// </summary>
    public class InteractionMnegTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Mneg_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Mneg");
                Assert.Equal(expected, e.Node.Mneg);
            }
        }
    }
}
