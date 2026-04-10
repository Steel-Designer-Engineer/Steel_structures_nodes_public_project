using System;
using System.Collections.Generic;
using steel_structures_nodes.Calculate.Models;
using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Тесты общего поведения <see cref="Calculator.BuildAnalysisTable"/> из проекта steel_structures_nodes.Calculate:
    /// обработка пустых входов, количество строк, объединение РСУ+РСН,
    /// фильтрация по элементам и сечениям, копирование всех усилий из одной комбинации.
    /// </summary>
    public class BuildAnalysisTableTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет: если на вход передать null для обоих списков (РСУ и РСН),
        /// метод не выбрасывает исключение и возвращает пустую коллекцию.
        /// Это граничный случай — защита от NullReferenceException.
        /// </summary>
        [Fact]
        public void NullInputs_ReturnsEmpty()
        {
            var table = CreateCalc().BuildAnalysisTable(null, null);
            Assert.NotNull(table);
            Assert.Empty(table);
        }

        /// <summary>
        /// Проверяет: пустые массивы (0 строк РСУ, 0 строк РСН) дают пустой результат.
        /// Расчёт невозможен без входных данных — таблица анализа должна быть пустой.
        /// </summary>
        [Fact]
        public void EmptyInputs_ReturnsEmpty()
        {
            var table = CreateCalc().BuildAnalysisTable(Array.Empty<ForceRow>(), Array.Empty<ForceRow>());
            Assert.Empty(table);
        }

        /// <summary>
        /// Проверяет: при наличии данных таблица анализа содержит ровно 11 строк MAX:
        /// MAX N, MAX N+, MAX N-, MAX Qy, MAX Qz, MAX Mx, MAX My, MAX Mz, MAX Mw, MAX Coeff, MAX u.
        /// Это фиксированная структура таблицы анализа РС1.
        /// </summary>
        [Fact]
        public void Returns10Rows()
        {
            var rsu = new List<ForceRow> { Make(n: "5", qy: "1", qz: "2", mx: "0.5", my: "3", mz: "1.5", mw: "0.1") };
            var table = CreateCalc().BuildAnalysisTable(rsu, Array.Empty<ForceRow>());
            Assert.Equal(11, table.Count);
        }

        /// <summary>
        /// Проверяет: строки из РСУ и РСН объединяются в общий пул перед расчётом.
        /// РСУ содержит N=10, РСН содержит N=50 > MAX N+ должен выбрать строку из РСН (50 > 10).
        /// Также проверяет, что LoadCombination указывает на источник (RSN1).
        /// </summary>
        [Fact]
        public void CombinesRsuAndRsn()
        {
            var rsu = new List<ForceRow> { Make(dcl: "RSU1", n: "10") };
            var rsn = new List<ForceRow> { Make(dcl: "RSN1", n: "50") };
            var table = CreateCalc().BuildAnalysisTable(rsu, rsn);
            Assert.Equal(50d, table[1].Nt);
            Assert.Equal("RSN1", table[1].LoadCombination);
        }

        /// <summary>
        /// Проверяет: фильтр элементов ограничивает пул данных.
        /// Есть два элемента (10, 20), фильтр пропускает только элемент "20".
        /// MAX N+ берётся только из строк элемента 20 > Nt=15.
        /// </summary>
        [Fact]
        public void ElementFilter_Applied()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", elem: "10", n: "5"),
                Make(dcl: "2", elem: "20", n: "15"),
            };
            var filter = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "20" };
            var table = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>(), filter);
            Assert.Equal(15d, table[1].Nt);
        }

        /// <summary>
        /// Проверяет: фильтр по сечениям оставляет только минимальное сечение (Sect=1).
        /// Строка с Sect=2 и N=99 отбрасывается, хотя N у неё больше.
        /// Среди оставшихся строк (Sect=1) MAX N+ = 20.
        /// Это соответствует логике: берём сечение у узла (обычно Sect=1).
        /// </summary>
        [Fact]
        public void FilterSection_KeepsMinSectionOnly()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", sect: "1", n: "10"),
                Make(dcl: "2", sect: "2", n: "99"),
                Make(dcl: "3", sect: "1", n: "20"),
            };
            var table = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            Assert.Equal(20d, table[1].Nt);
        }

        /// <summary>
        /// Проверяет: при выборе строки MAX копируются ВСЕ усилия из той же комбинации.
        /// Строка dcl="1" содержит N=100 (максимальное) и сопутствующие усилия.
        /// MAX N+ должен скопировать все поля целиком: Qy=11, Qz=22, Mx=33, My=44, Mz=55, Mw=66.
        /// Это ключевое требование: не смешивать значения из разных комбинаций нагрузок.
        /// </summary>
        [Fact]
        public void RowCopiesAllForcesFromSameCombination()
        {
            var rows = new List<ForceRow>
            {
                Make(dcl: "1", n: "100", qy: "11", qz: "22", mx: "33", my: "44", mz: "55", mw: "66"),
                Make(dcl: "2", n: "5"),
            };
            var table = CreateCalc().BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            var r = table[0]; // MAX N: max |N| = 100
            Assert.Equal("1", r.LoadCombination);
            Assert.Equal(100d, r.N);
            Assert.Equal(11d, r.Qy);
            Assert.Equal(22d, r.Qz);
            Assert.Equal(33d, r.Mx);
            Assert.Equal(44d, r.My);
            Assert.Equal(55d, r.Mz);
            Assert.Equal(66d, r.Mw);
        }
    }
}
