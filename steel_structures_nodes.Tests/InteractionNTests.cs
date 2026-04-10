using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты переменной N — продольная сила из interaction_tables.json.
    /// Проверяет что <see cref="InteractionTableService.LoadStandardNode"/> читает все значения N корректно.
    /// </summary>
    public class InteractionNTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_N_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "N");
                Assert.Equal(expected, e.Node.N);
            }
        }
    }
}
