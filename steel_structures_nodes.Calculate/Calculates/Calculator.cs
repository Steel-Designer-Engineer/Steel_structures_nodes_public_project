using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Calculate.Services;

namespace steel_structures_nodes.Calculate.Calculate {
    /// <summary>
    /// Калькулятор РС1: вычисление максимальных усилий и построение таблицы анализа
    /// с учётом данных несущей способности из альбома.
    /// </summary>
    public class Calculator : IRs1Calculator {
        private readonly IAlbumCapacityProvider _album;

        /// <summary>
        /// Создаёт калькулятор с провайдером альбома по умолчанию (автоматический поиск JSON-файла).
        /// </summary>
        public Calculator() : this(CreateDefaultAlbumProvider()) { }

        /// <summary>
        /// Создаёт калькулятор с указанным провайдером несущей способности.
        /// </summary>
        public Calculator(IAlbumCapacityProvider album) {
            _album = album;
        }

        private static IAlbumCapacityProvider CreateDefaultAlbumProvider() {
            try {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Prefer WPF project assets
                var dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 10 && dir != null; i++) {
                    var candidate = Path.Combine(dir.FullName, "steel_structures_nodes.Wpf", "Assets", "album_capacities.json");
                    if (File.Exists(candidate))
                        return new AlbumCapacityJsonProvider(candidate);

                    dir = dir.Parent;
                }

                // Fallback near exe
                var nearExe = Path.Combine(baseDir, "Assets", "album_capacities.json");
                if (File.Exists(nearExe))
                    return new AlbumCapacityJsonProvider(nearExe);
            }
            catch {
                // ignored
            }

            return new AlbumCapacityProvider();
        }

        /// <summary>
        /// Строит таблицу анализа РС1 из строк РСУ и РСН.
        /// Каждая входная строка — комбинация нагрузок с полным набором усилий.
        /// Для каждого критерия MAX выбирается строка ЦЕЛИКОМ (все усилия из одной комбинации).
        /// </summary>
        public IReadOnlyList<AnalysisRow> BuildAnalysisTable(IReadOnlyList<ForceRow> rsu, IReadOnlyList<ForceRow> rsn) {
            return BuildAnalysisTable(rsu, rsn, null);
        }

        /// <inheritdoc/>
        public IReadOnlyList<AnalysisRow> BuildAnalysisTable(IReadOnlyList<ForceRow> rsu, IReadOnlyList<ForceRow> rsn, ISet<string> elementFilter) {
            rsu = rsu ?? Array.Empty<ForceRow>();
            rsn = rsn ?? Array.Empty<ForceRow>();

            var table = new List<AnalysisRow>();
            if (rsu.Count == 0 && rsn.Count == 0)
                return table;

            // Album capacity key
            var cap = _album?.GetByKey("P1-P4-P6");

            // =====================================================
            // УНИВЕРСАЛЬНЫЙ АЛГОРИТМ (по формулам DCL(РСН) листа)
            // =====================================================
            // Входные данные: строки формата
            //   DclNo | [ElemType] | Elem | Sect | N | MX | MY | QZ | MZ | QY | MW
            //
            // Шаг 1: Пул данных — ВСЕ загруженные строки (РСУ + РСН).
            //         В Excel: VBA макросы "Вставить_РСУ" / "Вставить_РСН" / "Добавить"
            //         кладут данные в одну таблицу DCL(РСН).
            // Шаг 2: Фильтр элементов (пользователь выбирает нужные).
            // Шаг 3: Фильтр по последнему сечению (max Sect).
            // Шаг 4: Для каждой строки MAX — найти строку с экстремальным значением
            //         ведущего столбца, и вывести ВСЕ усилия из этой строки.

            // Шаг 1: объединяем все данные
            var pool = new List<ForceRow>(rsu.Count + rsn.Count);
            pool.AddRange(rsu);
            pool.AddRange(rsn);

            // Шаг 2: фильтр элементов
            if (elementFilter != null && elementFilter.Count > 0) {
                var filtered = pool.Where(r => elementFilter.Contains((r.Elem ?? "").Trim())).ToList();
                if (filtered.Count > 0)
                    pool = filtered;
            }

            // Шаг 3: только последнее сечение
            var data = FilterLastSection(pool);

            if (data.Count == 0)
                return table;

            // Строим строки MAX — каждая берёт строку ЦЕЛИКОМ по своему критерию.
            // Порядок: N > Qy > Qz > Mx > My > Mz > Mw > Coeff > u

            // MAX N: строка с max |N| (максимальная продольная сила, целиком)
            table.Add(BuildMaxByColumn("MAX N", data, f => f.ParsedN));

            // MAX N+: строка с max N > 0 (растяжение, целиком)
            table.Add(BuildMaxNtRow("MAX N+", data));

            // MAX N-: строка с min N < 0 (сжатие, целиком)
            table.Add(BuildMaxNcRow("MAX N-", data));

            // MAX Qy: строка с max |QY| (Qz > Qy, целиком)
            table.Add(BuildMaxByColumn("MAX Qy", data, f => f.ParsedQy));

            // MAX Qz: строка с max |QZ| (Qz > Qz, целиком)
            table.Add(BuildMaxByColumn("MAX Qz", data, f => f.ParsedQz));

            // MAX Mx: строка с max |MX| (крутящий момент, целиком)
            table.Add(BuildMaxByColumn("MAX Mx", data, f => f.ParsedMx));

            // MAX My: строка с max |MY| (AlbumMy > My, изгибающий момент, целиком)
            table.Add(BuildMaxByColumn("MAX My", data, f => f.ParsedMy));

            // MAX Mz: строка с max |MZ| (Mz > Mz, изгибающий момент, целиком)
            table.Add(BuildMaxByColumn("MAX Mz", data, f => f.ParsedMz));

            // MAX Mw: строка с max |MW| (целиком)
            table.Add(BuildMaxByColumn("MAX Mw", data, f => f.ParsedMw));

            // MAX Coeff: строка с max U = |Qz|/Qcap + |N|/Ncap (целиком)
            table.Add(BuildMaxByU("MAX Coeff", data, cap));

            // MAX u: строка с max U (целиком)
            table.Add(BuildMaxByU("MAX u", data, cap));

            // U и AlbumPsi для всех строк
            foreach (var r in table) {
                r.Psi = cap?.AlbumPsi;
                if (!r.U.HasValue)
                    r.U = CalcU(r, cap);
            }

            return table;
        }

        /// <summary>
        /// Строка с максимальным U = |Qz|/Qcap + |N|/Ncap.
        /// Берётся строка ЦЕЛИКОМ — все усилия из одной комбинации нагрузок.
        /// </summary>
        private static AnalysisRow BuildMaxByU(string name, IReadOnlyList<ForceRow> rows, ForceRow cap) {
            bool hasCap = cap != null && cap.AlbumQy != 0 && cap.AlbumNt.GetValueOrDefault() != 0;
            if (!hasCap || rows.Count == 0)
                return EmptyRow(name);

            int bestIdx = 0;
            double bestU = double.MinValue;

            for (int i = 0; i < rows.Count; i++) {
                var f = rows[i];
                var q = f.ParsedQz.HasValue ? Math.Abs(f.ParsedQz.Value) : 0d;
                var n = f.ParsedN.HasValue ? Math.Abs(f.ParsedN.Value) : 0d;
                var u = q / cap.AlbumQy + n / cap.AlbumNt.Value;
                if (u > bestU) { bestU = u; bestIdx = i; }
            }

            var row = RowToAnalysis(name, rows[bestIdx]);
            row.U = bestU;
            return row;
        }

        /// <summary>
        /// Универсальная строка MAX: найти строку с max |column|,
        /// вывести ВСЕ усилия из этой строки.
        /// Соответствует формулам Excel R7-R15 (INDEX/MATCH).
        /// </summary>
        private static AnalysisRow BuildMaxByColumn(
            string name,
            IReadOnlyList<ForceRow> rows,
            Func<ForceRow, double?> column) {
            var extremeAbs = MaxAbs(rows.Select(column));
            if (!extremeAbs.HasValue || extremeAbs.Value == 0)
                return EmptyRow(name);

            var idx = FindIndexByValue(rows, column, extremeAbs.Value);
            if (idx < 0)
                idx = FindIndexByValue(rows, column, -extremeAbs.Value);
            if (idx < 0)
                return EmptyRow(name);

            return RowToAnalysis(name, rows[idx]);
        }

        /// <summary>MAX AlbumNt: строка с максимальным положительным N.</summary>
        private static AnalysisRow BuildMaxNtRow(
            string name,
            IReadOnlyList<ForceRow> rows) {
            var max = MaxPositive(rows.Select(f => f.ParsedN));
            if (!max.HasValue)
                return EmptyRow(name);

            var idx = FindIndexByValue(rows, f => f.ParsedN, max.Value);
            if (idx < 0) return EmptyRow(name);

            var row = RowToAnalysis(name, rows[idx]);
            row.Nt = rows[idx].ParsedN;
            return row;
        }

        /// <summary>MAX AlbumN: строка с минимальным отрицательным N.</summary>
        private static AnalysisRow BuildMaxNcRow(
            string name,
            IReadOnlyList<ForceRow> rows) {
            var min = MinNegative(rows.Select(f => f.ParsedN));
            if (!min.HasValue)
                return EmptyRow(name);

            var idx = FindIndexByValue(rows, f => f.ParsedN, min.Value);
            if (idx < 0) return EmptyRow(name);

            var row = RowToAnalysis(name, rows[idx]);
            row.Nc = rows[idx].ParsedN;
            return row;
        }

        /// <summary>Создаёт строку анализа из найденной строки данных.</summary>
        private static AnalysisRow RowToAnalysis(string name, ForceRow r) {
            var n = r.ParsedN ?? 0d;
            return new AnalysisRow {
                RowType = name,
                LoadCombination = r.DclNo,
                Element = TryParseInt(r.Elem),
                N = r.ParsedN ?? 0d,
                Nt = n > 0 ? r.ParsedN : 0d,
                Nc = n < 0 ? r.ParsedN : 0d,
                Mx = r.ParsedMx ?? 0d,
                My = r.ParsedMy ?? 0d,
                Mz = r.ParsedMz ?? 0d,
                Mw = r.ParsedMw ?? 0d,
                Qy = r.ParsedQy ?? 0d,
                Qz = r.ParsedQz ?? 0d,
            };
        }

        /// <summary>Пустая строка для случая когда нет данных.</summary>
        private static AnalysisRow EmptyRow(string name) {
            return new AnalysisRow {
                RowType = name,
                LoadCombination = "0",
                Element = 0,
                N = 0d,
                Nt = 0d,
                Nc = 0d,
                Mx = 0d,
                My = 0d,
                Mz = 0d,
                Mw = 0d,
                Qy = 0d,
                Qz = 0d,
            };
        }
        private static double? CalcU(AnalysisRow row, ForceRow cap) {
            if (row == null || cap == null) return null;

            // Matches Excel sample: u = |Qz|/J3 + |N|/K3, where J3=Qcap, K3=Ncap
            if (cap.AlbumQy == 0 || cap.AlbumNt.GetValueOrDefault() == 0) return null;

            var q = row.Qz.HasValue ? Math.Abs(row.Qz.Value) : 0d;
            var n = row.N.HasValue ? Math.Abs(row.N.Value) : 0d;

            var u = q / cap.AlbumQy + n / cap.AlbumNt.Value;
            return u == 0 ? (double?)0d : u;
        }

        private static int FindIndexByValue(IReadOnlyList<ForceRow> rows, Func<ForceRow, double?> selector, double target) {
            for (int i = 0; i < rows.Count; i++) {
                var v = selector(rows[i]);
                if (!v.HasValue) continue;
                if (v.Value.Equals(target))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Если в строках заполнено поле Sect, оставляет только строки с первым (минимальным)
        /// сечением стержня. Для анализа узлового соединения используется сечение у узла
        /// (обычно Sect = 1), где действуют силы, передаваемые через соединение.
        /// Если Sect не заполнен ни у одной строки, возвращает все.
        /// </summary>
        private static IReadOnlyList<ForceRow> FilterLastSection(IReadOnlyList<ForceRow> rsu) {
            bool anySect = rsu.Any(r => !string.IsNullOrWhiteSpace(r.Sect));
            if (!anySect)
                return rsu;

            // Find the min section number (section at the node)
            int minSect = int.MaxValue;
            foreach (var r in rsu) {
                var s = (r.Sect ?? "").Trim();
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v < minSect)
                    minSect = v;
            }

            if (minSect == int.MaxValue)
                return rsu;

            var minSectStr = minSect.ToString(CultureInfo.InvariantCulture);
            var filtered = rsu.Where(r => {
                var s = (r.Sect ?? "").Trim();
                return s == minSectStr;
            }).ToList();

            return filtered.Count > 0 ? filtered : rsu;
        }

        private static int? TryParseInt(string s) {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                return v;
            return null;
        }
        private static double? MaxAbs(IEnumerable<double?> values) {
            double? max = null;
            foreach (var v in values) {
                if (!v.HasValue) continue;
                var a = Math.Abs(v.Value);
                if (!max.HasValue || a > max.Value)
                    max = a;
            }
            return max;
        }

        private static double? MaxPositive(IEnumerable<double?> values) {
            double? max = null;
            foreach (var v in values) {
                if (!v.HasValue) continue;
                if (v.Value <= 0) continue;
                if (!max.HasValue || v.Value > max.Value)
                    max = v.Value;
            }
            return max;
        }

        private static double? MinNegative(IEnumerable<double?> values) {
            double? min = null;
            foreach (var v in values) {
                if (!v.HasValue) continue;
                if (v.Value >= 0) continue;
                if (!min.HasValue || v.Value < min.Value)
                    min = v.Value;
            }
            return min;
        }

        /// <inheritdoc/>
        public ForceRow ExtractSummary(IReadOnlyList<AnalysisRow> analysisRows) {
            if (analysisRows == null) throw new ArgumentNullException(nameof(analysisRows));

            double? nAbs = null;
            double? nPlus = null;
            double? nMinus = null;
            double? qAbs = null;
            double? qzAbs = null;
            double? tAbs = null;
            double? mAbs = null;
            double? moAbs = null;
            double? mwAbs = null;
            double? maxU = null;
            double? psi = null;

            foreach (var a in analysisRows) {
                var rt = NormalizeRowType(a.RowType);

                if (rt == "MAX N" && a.N.HasValue)
                    nAbs = a.N.Value;
                else if (rt == "MAX N+" || rt == "MAX NT") {
                    if (a.Nt.HasValue && a.Nt.Value != 0)
                        nPlus = a.Nt.Value;
                    else if (a.N.HasValue && a.N.Value > 0)
                        nPlus = a.N.Value;
                }
                else if ((rt == "MAX N-" || rt == "MAX NC") && a.Nc.HasValue)
                    nMinus = a.Nc.Value;
                else if (rt == "MAX QY" && a.Qy.HasValue)
                    qAbs = Math.Abs(a.Qy.Value);
                else if (rt == "MAX QZ" && a.Qz.HasValue)
                    qzAbs = Math.Abs(a.Qz.Value);
                else if (rt == "MAX MX" && a.Mx.HasValue)
                    tAbs = Math.Abs(a.Mx.Value);
                else if (rt == "MAX MY" && a.My.HasValue)
                    mAbs = Math.Abs(a.My.Value);
                else if (rt == "MAX MZ" && a.Mz.HasValue)
                    moAbs = Math.Abs(a.Mz.Value);
                else if (rt == "MAX MW" && a.Mw.HasValue)
                    mwAbs = Math.Abs(a.Mw.Value);

                if (a.U.HasValue && (!maxU.HasValue || a.U.Value > maxU.Value))
                    maxU = a.U.Value;
                if (a.Psi.HasValue && !psi.HasValue)
                    psi = a.Psi.Value;
            }

            // Обнулить нулевые значения
            if (nAbs.HasValue && nAbs.Value == 0) nAbs = null;
            if (nPlus.HasValue && nPlus.Value == 0) nPlus = null;
            if (nMinus.HasValue && nMinus.Value == 0) nMinus = null;
            if (qAbs.HasValue && qAbs.Value == 0) qAbs = null;
            if (qzAbs.HasValue && qzAbs.Value == 0) qzAbs = null;
            if (tAbs.HasValue && tAbs.Value == 0) tAbs = null;
            if (mAbs.HasValue && mAbs.Value == 0) mAbs = null;
            if (moAbs.HasValue && moAbs.Value == 0) moAbs = null;
            if (mwAbs.HasValue && mwAbs.Value == 0) mwAbs = null;
            if (maxU.HasValue && maxU.Value == 0) maxU = null;

            return new ForceRow {
                SummaryN = nAbs,
                SummaryNt = nPlus,
                SummaryNc = nMinus,
                SummaryQy = qAbs,
                SummaryQz = qzAbs,
                SummaryMx = tAbs,
                SummaryMy = mAbs,
                SummaryMz = moAbs,
                SummaryMw = mwAbs,
                MaxU = maxU,
                SummaryPsi = psi,
            };
        }

        private static string NormalizeRowType(string s) {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return string.Join(" ", s.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
        }

        /// <inheritdoc/>
        public ForceRow CalculateAndSave(
            IReadOnlyList<ForceRow> rsu,
            IReadOnlyList<ForceRow> rsn,
            ISet<string> elementFilter,
            string resultDir,
            out string createdFilePath) {
            if (string.IsNullOrWhiteSpace(resultDir)) throw new ArgumentNullException(nameof(resultDir));

            rsu = rsu ?? Array.Empty<ForceRow>();
            rsn = rsn ?? Array.Empty<ForceRow>();

            if (!Directory.Exists(resultDir))
                Directory.CreateDirectory(resultDir);

            // Определяем следующий номер версии по существующим файлам
            int nextVersion = 1;
            foreach (var file in Directory.GetFiles(resultDir, "Result_v*.json")) {
                var name = Path.GetFileNameWithoutExtension(file);
                // Result_v001 -> "001"
                var idx = name.IndexOf("_v", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && int.TryParse(name.Substring(idx + 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) {
                    if (v >= nextVersion)
                        nextVersion = v + 1;
                }
            }

            // Калькулятор считает расчётные данные
            var analysis = BuildAnalysisTable(rsu, rsn, elementFilter);
            var summary = ExtractSummary(analysis);

            // Заполняем результат в ForceRow (summary уже ForceRow)
            summary.Version = nextVersion;
            summary.AnalysisRows = new List<AnalysisRow>(analysis);

            // Калькулятор записывает расчётные данные в отдельный JSON-файл с версией
            var json = Rs1ResultJsonSerializer.ToJson(summary);
            createdFilePath = Path.Combine(resultDir, $"Result_v{nextVersion:D3}.json");
            File.WriteAllText(createdFilePath, json, System.Text.Encoding.UTF8);

            return summary;
        }
    }
}

