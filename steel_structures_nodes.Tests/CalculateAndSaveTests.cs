using System;
using System.Collections.Generic;
using System.IO;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Calculate.Services;
using Xunit;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Тесты метода <see cref="Calculator.CalculateAndSave"/> из проекта steel_structures_nodes.Calculate —
    /// полный цикл: расчёт > сериализация в JSON > сохранение на диск.
    /// Проверяется создание файлов, инкремент версий и корректность round-trip (JSON > объект > JSON).
    /// </summary>
    public class CalculateAndSaveTests : CalculatorTestBase
    {
        /// <summary>
        /// Проверяет:
        /// 1) Первый вызов создаёт файл Result_v001.json и возвращает Version=1.
        /// 2) Второй вызов создаёт Result_v002.json (инкремент по существующим файлам).
        /// Версии определяются автоматически по файлам в каталоге результатов.
        /// </summary>
        [Fact]
        public void WritesFile_And_IncrementsVersion()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "HJ_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var rsu = new List<ForceRow> { Make(dcl: "1", n: "5") };
                var rsn = new List<ForceRow> { Make(dcl: "2", n: "10") };
                var calc = CreateCalc();

                // Первый расчёт > v001
                var r1 = calc.CalculateAndSave(rsu, rsn, null, tmp, out var p1);
                Assert.True(File.Exists(p1));
                Assert.Contains("Result_v001", Path.GetFileName(p1));
                Assert.Equal(1, r1.Version);

                // Второй расчёт > v002 (инкремент)
                var r2 = calc.CalculateAndSave(rsu, rsn, null, tmp, out var p2);
                Assert.Contains("Result_v002", Path.GetFileName(p2));
                Assert.Equal(2, r2.Version);
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }

        /// <summary>
        /// Проверяет round-trip: данные, записанные в JSON, можно прочитать обратно
        /// и получить те же значения. Контролируется:
         /// - Version=1 (первый файл),
         /// - 11 строк анализа (фиксированная структура),
         /// - SummaryNt=50 (максимальный положительный N из входных данных),
         /// - SummaryNc=-30 (минимальный отрицательный N).
         /// </summary>
        [Fact]
        public void JsonRoundtrip_PreservesData()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "HJ_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var rsu = new List<ForceRow>
                {
                    Make(dcl: "1", n: "50", qy: "11", qz: "22", mx: "33", my: "44", mz: "55", mw: "66"),
                    Make(dcl: "2", n: "-30"),
                };
                CreateCalc(qy: 10, nt: 100, psi: 0.9).CalculateAndSave(rsu, Array.Empty<ForceRow>(), null, tmp, out var path);

                var restored = Rs1ResultJsonSerializer.FromJson(File.ReadAllText(path));
                Assert.Equal(1, restored.Version);
                Assert.Equal(11, restored.AnalysisRows.Count);
                Assert.Equal(50d, restored.SummaryNt);
                Assert.Equal(-30d, restored.SummaryNc);
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }
    }
}
