using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using steel_structures_nodes.Calculate.Models;
using steel_structures_nodes.Calculate.Models.RSN;
using steel_structures_nodes.Calculate.Models.RSU;
using steel_structures_nodes.Calculate.Services;

namespace steel_structures_nodes.Wpf.Services
{
    /// <summary>
    /// Реализация <see cref="IExcelReader"/> на основе библиотеки EPPlus для чтения Excel-файлов.
    /// </summary>
    public class EpplusExcelReader : IExcelReader
    {
        public EpplusExcelReader()
        {
            // Non-commercial usage (see packages/EPPlus.* readme)
            ExcelPackage.License.SetNonCommercialPersonal("Sharnir");
        }

        public ExcelWorkbookInfo GetWorkbookInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                List<string> names = package.Workbook.Worksheets.Select(w => w.Name).ToList();
                return new ExcelWorkbookInfo(names);
            }
        }

        public List<ForceRow> ReadRsuRsn(string filePath, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (string.IsNullOrWhiteSpace(sheetName)) throw new ArgumentNullException(nameof(sheetName));

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var ws = package.Workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
                if (ws == null)
                    throw new InvalidOperationException("Sheet not found: " + sheetName);

                var dim = ws.Dimension;
                if (dim == null)
                    return new List<ForceRow>();

                int rowCount = dim.End.Row;
                int colCount = dim.End.Column;

                if (string.Equals(sheetName, "Элементы", StringComparison.OrdinalIgnoreCase))
                {
                    var barsHeaderMap = BuildHeaderMap(ws, headerRow: 1, colCount: colCount);

                    int cDcl = TryHeader(barsHeaderMap, "Номер РСН");
                    if (cDcl < 0) cDcl = TryHeader(barsHeaderMap, "DCL No");

                    int cN = TryHeader(barsHeaderMap, "N. кН");
                    if (cN < 0) cN = TryHeader(barsHeaderMap, "N, кН");

                    int cMx = TryHeader(barsHeaderMap, "MK. кН*м");
                    if (cMx < 0) cMx = TryHeader(barsHeaderMap, "MX, кН*м");

                    int cMy = TryHeader(barsHeaderMap, "MY. кН*м");
                    if (cMy < 0) cMy = TryHeader(barsHeaderMap, "MY, кН*м");

                    int cQz = TryHeader(barsHeaderMap, "QZ. кН");
                    if (cQz < 0) cQz = TryHeader(barsHeaderMap, "QZ, кН");

                    int cMz = TryHeader(barsHeaderMap, "MZ. кН*м");
                    if (cMz < 0) cMz = TryHeader(barsHeaderMap, "MZ, кН*м");

                    int cQy = TryHeader(barsHeaderMap, "QY. кН");
                    if (cQy < 0) cQy = TryHeader(barsHeaderMap, "QY, кН");

                    int cMw = TryHeader(barsHeaderMap, "MW. кН*м*м");

                    if (cN < 0 || cMx < 0 || cMy < 0 || cQz < 0 || cMz < 0 || cQy < 0)
                        throw new InvalidOperationException("Required force columns not found.");

                    var bars = new List<ForceRow>();
                    for (int r = 2; r <= rowCount; r++)
                    {
                        var n = GetText(ws, r, cN);
                        var mx = GetText(ws, r, cMx);
                        var my = GetText(ws, r, cMy);
                        var qz = GetText(ws, r, cQz);
                        var mz = GetText(ws, r, cMz);
                        var qy = GetText(ws, r, cQy);
                        var mw = cMw > 0 ? GetText(ws, r, cMw) : string.Empty;

                        var dclRaw = cDcl > 0 ? GetText(ws, r, cDcl) : string.Empty;
                        if (IsAllEmpty(n, mx, my, qz, mz, qy, mw) && string.IsNullOrWhiteSpace(dclRaw))
                            continue;

                        bars.Add(new RsuRow
                        {
                            DclNo = NormalizeDclNo(dclRaw),
                            N = NormalizeNumberText(n),
                            Mx = NormalizeNumberText(mx),
                            My = NormalizeNumberText(my),
                            Qz = NormalizeNumberText(qz),
                            Mz = NormalizeNumberText(mz),
                            Qy = NormalizeNumberText(qy),
                            Mw = NormalizeNumberText(mw),
                        });
                    }

                    return bars;
                }

                int best = FindBestHeaderRow(ws, maxRowsToScan: Math.Min(80, rowCount), colCount: colCount);
                var map = BuildRsuRsnHeaderMap(ws, best, colCount);
                if (!map.IsValid)
                    throw new InvalidOperationException("RSU/RSN header mapping failed on row " + best + ". " + map);

                var rsuDetectHeaderMap = BuildHeaderMap(ws, headerRow: map.HeaderRow, colCount: colCount);
                bool isRsu = TryHeader(rsuDetectHeaderMap, "ЗАГРУЖЕНИЯ") > 0;

                var rows = new List<ForceRow>();
                for (int r = map.HeaderRow + 1; r <= rowCount; r++)
                {
                    var dclRaw = map.ColLoadCase > 0 ? GetText(ws, r, map.ColLoadCase) : string.Empty;
                    var dcl = isRsu ? ExpandArithmeticSequence(NormalizeDclNo(dclRaw)) : NormalizeDclNo(dclRaw);

                    var elem = map.ColElem > 0 ? GetText(ws, r, map.ColElem) : string.Empty;

                    var n = map.ColN > 0 ? GetText(ws, r, map.ColN) : string.Empty;
                    var mx = map.ColMx > 0 ? GetText(ws, r, map.ColMx) : string.Empty;
                    var my = map.ColMy > 0 ? GetText(ws, r, map.ColMy) : string.Empty;
                    var qz = map.ColQz > 0 ? GetText(ws, r, map.ColQz) : string.Empty;
                    var mz = map.ColMz > 0 ? GetText(ws, r, map.ColMz) : string.Empty;
                    var qy = map.ColQy > 0 ? GetText(ws, r, map.ColQy) : string.Empty;
                    var mw = map.ColMw > 0 ? GetText(ws, r, map.ColMw) : string.Empty;

                    if (IsAllEmpty(dcl, elem, n, mx, my, qz, mz, qy, mw))
                        continue;

                    rows.Add(new RsnRow
                    {
                        DclNo = dcl,
                        ElemType = map.ColElemType > 0 ? GetText(ws, r, map.ColElemType) : string.Empty,
                        Elem = elem,
                        Sect = map.ColSect > 0 ? GetText(ws, r, map.ColSect) : string.Empty,
                        N = NormalizeNumberText(n),
                        Mx = NormalizeNumberText(mx),
                        My = NormalizeNumberText(my),
                        Qz = NormalizeNumberText(qz),
                        Mz = NormalizeNumberText(mz),
                        Qy = NormalizeNumberText(qy),
                        Mw = NormalizeNumberText(mw),
                    });
                }

                return rows;
            }
        }

        private static string ExpandArithmeticSequence(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            // Expand sequences like "1...4 9 10 12" -> "1 2 3 4 9 10 12".
            // Applies only to RSU.
            var tokens = raw
                .Replace(',', ' ')
                .Replace(';', ' ')
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var numbers = new SortedSet<int>();
            var others = new List<string>();

            foreach (var t in tokens)
            {
                var tok = t.Trim();
                if (tok.Length == 0)
                    continue;

                int dots = tok.IndexOf("...", StringComparison.Ordinal);
                if (dots > 0)
                {
                    var left = tok.Substring(0, dots);
                    var right = tok.Substring(dots + 3);
                    if (int.TryParse(left, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a)
                        && int.TryParse(right, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
                    {
                        if (a <= b)
                        {
                            for (int i = a; i <= b; i++) numbers.Add(i);
                        }
                        else
                        {
                            for (int i = a; i >= b; i--) numbers.Add(i);
                        }
                        continue;
                    }
                }

                if (int.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    numbers.Add(n);
                    continue;
                }

                others.Add(tok);
            }

            var sb = new StringBuilder();
            foreach (var n in numbers)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(n.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var o in others)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(o);
            }

            return sb.ToString();
        }

        private static int FindBestHeaderRow(ExcelWorksheet ws, int maxRowsToScan, int colCount)
        {
            int bestRow = 1;
            int bestScore = -1;

            for (int r = 1; r <= maxRowsToScan; r++)
            {
                var headerMap = BuildHeaderMap(ws, headerRow: r, colCount: colCount);

                int score = 0;
                if (TryHeader(headerMap, "DCL No") > 0 || TryHeader(headerMap, "ЗАГРУЖЕНИЯ") > 0 || TryHeader(headerMap, "Номер РСН") > 0) score++;
                if (TryHeader(headerMap, "ЭЛЕМ") > 0 || TryHeader(headerMap, "ELEM") > 0) score++;

                if (TryPrefixHeader(headerMap, "N") > 0) score++;
                if (TryPrefixHeader(headerMap, "MK") > 0) score++;
                if (TryPrefixHeader(headerMap, "QZ") > 0) score++;
                if (TryPrefixHeader(headerMap, "MZ") > 0) score++;
                if (TryPrefixHeader(headerMap, "QY") > 0) score++;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestRow = r;
                }
            }

            return bestRow;
        }

        private static RsuRsnHeaderMap BuildRsuRsnHeaderMap(ExcelWorksheet ws, int headerRow, int colCount)
        {
            var headerMap = BuildHeaderMap(ws, headerRow: headerRow, colCount: colCount);

            return new RsuRsnHeaderMap
            {
                HeaderRow = headerRow,

                ColLoadCase = FirstPositive(
                    TryHeader(headerMap, "ЗАГРУЖЕНИЯ"),
                    TryHeader(headerMap, "Номер РСН"),
                    TryHeader(headerMap, "DCL No"),
                    TryHeader(headerMap, "DCLNo")),

                ColElem = FirstPositive(
                    TryHeader(headerMap, "ЭЛЕМ"),
                    TryHeader(headerMap, "ELEM")),

                ColSect = FirstPositive(
                    TryHeader(headerMap, "SECT"),
                    TryHeader(headerMap, "СЕЧ"),
                    TryHeader(headerMap, "СЕЧ."),
                    TryHeader(headerMap, "НС")),

                ColElemType = FirstPositive(
                    TryHeader(headerMap, "КоЭ/Тип КЭ"),
                    TryHeader(headerMap, "КоЭТипКЭ"),
                    TryHeader(headerMap, "ELEM type"),
                    TryHeader(headerMap, "ELEMtype")),

                ColN = FirstPositive(TryPrefixHeader(headerMap, "N"), TryHeader(headerMap, "N")),
                ColMx = FirstPositive(TryPrefixHeader(headerMap, "MK"), TryPrefixHeader(headerMap, "MX"), TryHeader(headerMap, "MK")),
                ColMy = FirstPositive(TryPrefixHeader(headerMap, "MY"), TryHeader(headerMap, "MY")),
                ColQz = FirstPositive(TryPrefixHeader(headerMap, "QZ"), TryHeader(headerMap, "QZ")),
                ColMz = FirstPositive(TryPrefixHeader(headerMap, "MZ"), TryHeader(headerMap, "MZ")),
                ColQy = FirstPositive(TryPrefixHeader(headerMap, "QY"), TryHeader(headerMap, "QY")),
                ColMw = FirstPositive(TryPrefixHeader(headerMap, "MW"), TryHeader(headerMap, "MW")),
            };
        }

        private static int TryPrefixHeader(Dictionary<string, int> headerMap, string prefix)
        {
            var p = NormalizeHeader(prefix);
            foreach (var kv in headerMap)
            {
                if (kv.Key.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return -1;
        }

        private static int FirstPositive(params int[] values)
        {
            foreach (var v in values)
                if (v > 0) return v;
            return 0;
        }

        private static string GetText(ExcelWorksheet ws, int row, int col)
        {
            var v = ws.Cells[row, col].Value;
            if (v == null) return string.Empty;
            return Convert.ToString(v);
        }

        private static bool IsAllEmpty(params string[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrWhiteSpace(v))
                    return false;
            return true;
        }

        private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet ws, int headerRow, int colCount)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= colCount; c++)
            {
                var t = GetText(ws, headerRow, c);
                if (string.IsNullOrWhiteSpace(t))
                    continue;

                var key = NormalizeHeader(t);
                if (!map.ContainsKey(key))
                    map[key] = c;
            }
            return map;
        }

        private static int TryHeader(Dictionary<string, int> headerMap, string header)
        {
            var key = NormalizeHeader(header);
            if (headerMap.TryGetValue(key, out var idx))
                return idx;

            var alternatives = new[]
            {
                header.Replace(',', '.'),
                header.Replace('.', ','),
                header.Replace("*", "?"),
                header.Replace("?", "*")
            }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(NormalizeHeader);

            foreach (var alt in alternatives)
                if (headerMap.TryGetValue(alt, out idx))
                    return idx;

            return -1;
        }

        private static string NormalizeHeader(string s)
        {
            var t = (s ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace(" ", string.Empty)
                .Replace("\t", string.Empty);

            t = t.Replace(".", string.Empty).Replace(",", string.Empty);
            t = t.Replace("?", "*");
            return t;
        }

        private static string NormalizeDclNo(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            var t = s.Trim();
            if (t.Contains("=") || t.Contains("|") || t.Contains("\t"))
                return FirstToken(t);

            return t;
        }

        private static string FirstToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;

            var t = s.Trim();
            int i = 0;
            while (i < t.Length && !char.IsWhiteSpace(t[i]))
                i++;
            return i == 0 ? string.Empty : t.Substring(0, i);
        }

        private static string NormalizeNumberText(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;
            return s.Trim().Replace(',', '.');
        }
    }
}
