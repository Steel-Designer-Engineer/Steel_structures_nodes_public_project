using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты коэффициентов взаимодействия (α, β, γ, δ, ε, λ) из interaction_tables.json.
    /// </summary>
    public class InteractionGreekCoeffTests : InteractionTestBase
    {
        [Fact]
        public void AllEntries_Alpha_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "α");
                Assert.Equal(expected, e.Node.Alpha);
            }
        }

        [Fact]
        public void AllEntries_Beta_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "β");
                Assert.Equal(expected, e.Node.Beta);
            }
        }

        [Fact]
        public void AllEntries_Gamma_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "γ");
                Assert.Equal(expected, e.Node.Gamma);
            }
        }

        [Fact]
        public void AllEntries_Delta_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "δ");
                Assert.Equal(expected, e.Node.Delta);
            }
        }

        [Fact]
        public void AllEntries_Epsilon_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "ε");
                Assert.Equal(expected, e.Node.Epsilon);
            }
        }

        [Fact]
        public void AllEntries_Lambda_MatchRawJson()
        {
            Assert.NotEmpty(AllEntries);
            foreach (var e in AllEntries)
            {
                var expected = RawDouble(e.Raw, "λ");
                Assert.Equal(expected, e.Node.Lambda);
            }
        }
    }
}
