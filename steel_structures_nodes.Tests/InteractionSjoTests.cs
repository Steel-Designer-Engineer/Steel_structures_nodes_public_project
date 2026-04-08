using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты переменной Sjo — начальная жёсткость (Sjo) из interaction_tables.json.
    /// </summary>
    public class InteractionSjoTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Sjo_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "Sjo");
                Assert.Equal(expected, e.Node.Sjo);
            }
        }
    }
}
