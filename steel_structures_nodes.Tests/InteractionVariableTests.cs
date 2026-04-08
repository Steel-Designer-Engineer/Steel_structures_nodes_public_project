using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной variable из interaction_tables.json.
    /// </summary>
    public class InteractionVariableTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Variable_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "variable");
                Assert.Equal(expected, e.Node.Variable);
            }
        }
    }
}
