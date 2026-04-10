using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using steel_structures_nodes.Wpf.Models;
using steel_structures_nodes.Wpf.Services;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// Базовый класс для тестов данных interaction_tables.json.
    /// Загружает все записи (Name + CONNECTION_CODE → StandardNodeData) через <see cref="InteractionTableService"/>
    /// и строит параллельные списки «сырых» значений из JSON через <see cref="SimpleJsonParser"/>.
    /// </summary>
    public abstract class InteractionTestBase
    {
        /// <summary>Одна запись: ключ (Name, Code) + данные, прочитанные программой + сырой словарь из JSON.</summary>
        protected sealed class Entry
        {
            public string Name { get; init; }
            public string Code { get; init; }
            public StandardNodeData Node { get; init; }
            public Dictionary<string, object> Raw { get; init; }
        }

        protected static readonly List<Entry> AllEntries;
        protected static readonly SimpleJsonParser Parser;

        static InteractionTestBase()
        {
            var path = ResolveTestAssetPath("interaction_tables.json");
            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            Parser = new SimpleJsonParser(json);

            AllEntries = [];

            // Один проход по JSON-массиву: строим Entry напрямую из raw-словаря.
            // Это O(N) вместо O(N²), которое было при вызове svc.LoadStandardNode для каждой записи.
            var allRows = Parser.TryReadAllInteractionRows();
            foreach (var (name, code, raw) in allRows)
            {
                var node = BuildNodeFromRaw(Parser, raw);
                AllEntries.Add(new Entry
                {
                    Name = name,
                    Code = code,
                    Node = node,
                    Raw = raw,
                });
            }
        }

        private static StandardNodeData BuildNodeFromRaw(SimpleJsonParser p, Dictionary<string, object> row)
        {
            return new StandardNodeData
            {
                ProfileBeam = p.TryGetNestedString(row, "Geometry.Beam.ProfileBeam") ?? p.TryGetString(row, "ProfileBeam"),
                ProfileColumn = p.TryGetNestedString(row, "Geometry.Column.ProfileColumn") ?? p.TryGetString(row, "ProfileColumn"),
                Sj = p.TryGetNestedDouble(row, "Stiffness.Sj") ?? p.TryGetNullableDouble(row, "Sj"),
                Sjo = p.TryGetNestedDouble(row, "Stiffness.Sjo") ?? p.TryGetNullableDouble(row, "Sjo"),
                Variable = p.TryGetNullableDouble(row, "variable"),
                N = p.TryGetNestedDouble(row, "InternalForces.N") ?? p.TryGetNullableDouble(row, "N"),
                Nt = p.TryGetNestedDouble(row, "InternalForces.Nt") ?? p.TryGetNullableDouble(row, "Nt"),
                Nc = p.TryGetNestedDouble(row, "InternalForces.Nc") ?? p.TryGetNullableDouble(row, "Nc"),
                My = p.TryGetNestedDouble(row, "InternalForces.My") ?? p.TryGetNullableDouble(row, "My"),
                Mz = p.TryGetNestedDouble(row, "InternalForces.Mz") ?? p.TryGetNullableDouble(row, "Mz"),
                Mx = p.TryGetNestedDouble(row, "InternalForces.Mx") ?? p.TryGetNullableDouble(row, "Mx"),
                Mw = p.TryGetNestedDouble(row, "InternalForces.Mw") ?? p.TryGetNullableDouble(row, "Mw"),
                T = p.TryGetNestedDouble(row, "InternalForces.T") ?? p.TryGetNullableDouble(row, "T"),
                Mneg = p.TryGetNestedDouble(row, "InternalForces.Mneg") ?? p.TryGetNullableDouble(row, "Mneg"),
                Qy = p.TryGetNestedDouble(row, "InternalForces.Qy") ?? p.TryGetNullableDouble(row, "Qy"),
                Qz = p.TryGetNestedDouble(row, "InternalForces.Qz") ?? p.TryGetNullableDouble(row, "Qz"),
                Alpha = p.TryGetNestedDouble(row, "Coefficients.Alpha") ?? p.TryGetNullableDouble(row, "α"),
                Beta = p.TryGetNestedDouble(row, "Coefficients.Beta") ?? p.TryGetNullableDouble(row, "β"),
                Gamma = p.TryGetNestedDouble(row, "Coefficients.Gamma") ?? p.TryGetNullableDouble(row, "γ"),
                Delta = p.TryGetNestedDouble(row, "Coefficients.Delta") ?? p.TryGetNullableDouble(row, "δ"),
                Epsilon = p.TryGetNestedDouble(row, "Coefficients.Epsilon") ?? p.TryGetNullableDouble(row, "ε"),
                Lambda = p.TryGetNestedDouble(row, "Coefficients.Lambda") ?? p.TryGetNullableDouble(row, "λ"),
                BeamH = p.TryGetNestedDouble(row, "Geometry.Beam.Beam_H") ?? p.TryGetNullableDouble(row, "H"),
                BeamB = p.TryGetNestedDouble(row, "Geometry.Beam.Beam_B") ?? p.TryGetNullableDouble(row, "B"),
                BeamS = p.TryGetNestedDouble(row, "Geometry.Beam.Beam_s") ?? p.TryGetNullableDouble(row, "s"),
                BeamT = p.TryGetNestedDouble(row, "Geometry.Beam.Beam_t") ?? p.TryGetNullableDouble(row, "t"),
            };
        }

        /// <summary>
        /// Ищет JSON-файл: сначала в Assets тестового проекта (bin/Assets/),
        /// затем через <see cref="JsonAssetPathResolver"/> в WPF-проекте.
        /// </summary>
        private static string ResolveTestAssetPath(string fileName)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var testAsset = Path.Combine(baseDir, "Assets", fileName);
            if (File.Exists(testAsset))
                return testAsset;

            return JsonAssetPathResolver.ResolveWpfAssetsJsonPath(fileName);
        }

        /// <summary>
        /// Извлекает double? из сырого словаря JSON по ключу.
        /// </summary>
        protected static double? RawDouble(Dictionary<string, object> raw, string key)
        {
            return Parser.TryGetNestedDouble(raw, key) ?? Parser.TryGetNullableDouble(raw, key);
        }
    }
}
