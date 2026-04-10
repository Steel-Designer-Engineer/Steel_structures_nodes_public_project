using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using steel_structures_nodes.Calculate.Calculate;
using steel_structures_nodes.Calculate.Models;

namespace steel_structures_nodes.Calculate.Services
{
    /// <summary>
    /// Сериализация / десериализация результата расчёта РС1 (<see cref="ForceRow"/>) в JSON (Result_vXXX.json).
    /// Калькулятор записывает расчётные данные через ToJson, UI читает через FromJson.
    /// </summary>
    public static class Rs1ResultJsonSerializer
    {
        // ????????????? Сериализация ?????????????

        /// <summary>
        /// Сериализует результат расчёта в JSON.
        /// </summary>
        /// <remarks>
        /// Структура JSON:
        /// <list type="bullet">
        /// <item><description><c>version</c> — версия расчёта</description></item>
        /// <item><description><c>summary</c> — сводка (экстремальные значения)</description></item>
        /// <item><description><c>analysisRows</c> — таблица анализа (MAX-строки)</description></item>
        /// </list>
        /// </remarks>
        public static string ToJson(ForceRow result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            var sb = new StringBuilder();
            sb.Append('{');

            // Version
            sb.Append("\"version\":");
            sb.Append(result.Version.ToString(CultureInfo.InvariantCulture));

            // Summary
            sb.Append(",\"summary\":");
            WriteSummary(sb, result);

            // AnalysisRows
            sb.Append(",\"analysisRows\":[");
            if (result.AnalysisRows != null)
            {
                for (int i = 0; i < result.AnalysisRows.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    WriteRow(sb, result.AnalysisRows[i]);
                }
            }
            sb.Append(']');

            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Записывает объект <c>summary</c> для JSON результата.
        /// </summary>
        private static void WriteSummary(StringBuilder sb, ForceRow s)
        {
            if (s == null) { sb.Append("null"); return; }

            sb.Append('{');
            WriteJsonDouble(sb, "SummaryN", s.SummaryN); sb.Append(',');
            WriteJsonDouble(sb, "SummaryNt", s.SummaryNt); sb.Append(',');
            WriteJsonDouble(sb, "SummaryNc", s.SummaryNc); sb.Append(',');
            WriteJsonDouble(sb, "SummaryQy", s.SummaryQy); sb.Append(',');
            WriteJsonDouble(sb, "SummaryQz", s.SummaryQz); sb.Append(',');
            WriteJsonDouble(sb, "SummaryMx", s.SummaryMx); sb.Append(',');
            WriteJsonDouble(sb, "SummaryMy", s.SummaryMy); sb.Append(',');
            WriteJsonDouble(sb, "SummaryMz", s.SummaryMz); sb.Append(',');
            WriteJsonDouble(sb, "SummaryMw", s.SummaryMw); sb.Append(',');
            WriteJsonDouble(sb, "MaxU", s.MaxU); sb.Append(',');
            WriteJsonDouble(sb, "Psi", s.SummaryPsi);
            sb.Append('}');
        }

        /// <summary>
        /// Записывает одну строку таблицы анализа в JSON.
        /// Формат: {"RowType":"...", "LoadCombination":"...", "Element":..., усилия...}
        /// </summary>
        private static void WriteRow(StringBuilder sb, AnalysisRow r)
        {
            if (r == null) { sb.Append("null"); return; }

            sb.Append('{');
            WriteJsonString(sb, "RowType", r.RowType); sb.Append(',');
            WriteJsonString(sb, "LoadCombination", r.LoadCombination); sb.Append(',');
            WriteJsonInt(sb, "Element", r.Element); sb.Append(',');
            WriteJsonDouble(sb, "N", r.N); sb.Append(',');
            WriteJsonDouble(sb, "Nt", r.Nt); sb.Append(',');
            WriteJsonDouble(sb, "Nc", r.Nc); sb.Append(',');
            WriteJsonDouble(sb, "Qy", r.Qy); sb.Append(',');
            WriteJsonDouble(sb, "Qz", r.Qz); sb.Append(',');
            WriteJsonDouble(sb, "Mx", r.Mx); sb.Append(',');
            WriteJsonDouble(sb, "My", r.My); sb.Append(',');
            WriteJsonDouble(sb, "Mz", r.Mz); sb.Append(',');
            WriteJsonDouble(sb, "Mw", r.Mw); sb.Append(',');
            WriteJsonDouble(sb, "U", r.U); sb.Append(',');
            WriteJsonDouble(sb, "Psi", r.Psi);
            sb.Append('}');
        }

        /// <summary>Записывает JSON-свойство с числовым значением double: "name":value или "name":null</summary>
        private static void WriteJsonDouble(StringBuilder sb, string name, double? v)
        {
            sb.Append('"').Append(name).Append("\":");
            if (v.HasValue)
                sb.Append(v.Value.ToString("G", CultureInfo.InvariantCulture));
            else
                sb.Append("null");
        }

        /// <summary>Записывает JSON-свойство со строковым значением: "name":"value" или "name":null</summary>
        private static void WriteJsonString(StringBuilder sb, string name, string v)
        {
            sb.Append('"').Append(name).Append("\":");
            if (v == null)
                sb.Append("null");
            else
                sb.Append('"').Append(EscapeJson(v)).Append('"');
        }

        /// <summary>Записывает JSON-свойство с целочисленным значением: "name":value или "name":null</summary>
        private static void WriteJsonInt(StringBuilder sb, string name, int? v)
        {
            sb.Append('"').Append(name).Append("\":");
            if (v.HasValue)
                sb.Append(v.Value.ToString(CultureInfo.InvariantCulture));
            else
                sb.Append("null");
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        // ????????????? Десериализация ?????????????

        /// <summary>
        /// Десериализует JSON результата расчёта в <see cref="ForceRow"/>.
        /// </summary>
        public static ForceRow FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentNullException(nameof(json));

            var p = new JsonParser(json);
            return p.ReadResult();
        }

        /// <summary>
        /// Минимальный JSON-парсер для чтения Result.json.
        /// </summary>
        private sealed class JsonParser
        {
            private readonly string _s;
            private int _pos;

            public JsonParser(string s) { _s = s; _pos = 0; }

            /// <summary>
            /// Читает корневой объект JSON результата.
            /// </summary>
            public ForceRow ReadResult()
            {
                var result = new ForceRow();
                SkipWs();
                Expect('{');
                SkipWs();

                while (Peek() != '}')
                {
                    var key = ReadString();
                    SkipWs();
                    Expect(':');
                    SkipWs();

                    if (string.Equals(key, "version", StringComparison.OrdinalIgnoreCase))
                    {
                        var v = ReadNullableInt();
                        result.Version = v ?? 0;
                    }
                    else if (string.Equals(key, "summary", StringComparison.OrdinalIgnoreCase))
                        ReadSummaryInto(result);
                    else if (string.Equals(key, "analysisRows", StringComparison.OrdinalIgnoreCase))
                        result.AnalysisRows = ReadRowArray();
                    else
                        SkipValue();

                    SkipWs();
                    if (Peek() == ',') { _pos++; SkipWs(); }
                }
                Expect('}');
                return result;
            }

            /// <summary>
            /// Читает объект <c>summary</c> и записывает значения в переданный <see cref="ForceRow"/>.
            /// </summary>
            private void ReadSummaryInto(ForceRow s)
            {
                if (TryNull()) return;

                Expect('{');
                SkipWs();
                while (Peek() != '}')
                {
                    var key = ReadString();
                    SkipWs(); Expect(':'); SkipWs();

                    if (string.Equals(key, "SummaryN", StringComparison.OrdinalIgnoreCase)) s.SummaryN = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryNt", StringComparison.OrdinalIgnoreCase)) s.SummaryNt = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryNc", StringComparison.OrdinalIgnoreCase)) s.SummaryNc = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryQy", StringComparison.OrdinalIgnoreCase)) s.SummaryQy = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryQz", StringComparison.OrdinalIgnoreCase)) s.SummaryQz = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryMx", StringComparison.OrdinalIgnoreCase)) s.SummaryMx = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryMy", StringComparison.OrdinalIgnoreCase)) s.SummaryMy = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryMz", StringComparison.OrdinalIgnoreCase)) s.SummaryMz = ReadNullableDouble();
                    else if (string.Equals(key, "SummaryMw", StringComparison.OrdinalIgnoreCase)) s.SummaryMw = ReadNullableDouble();
                    else if (string.Equals(key, "MaxU", StringComparison.OrdinalIgnoreCase)) s.MaxU = ReadNullableDouble();
                    else if (string.Equals(key, "Psi", StringComparison.OrdinalIgnoreCase)) s.SummaryPsi = ReadNullableDouble();
                    else SkipValue();

                    SkipWs();
                    if (Peek() == ',') { _pos++; SkipWs(); }
                }
                Expect('}');
            }

            private List<AnalysisRow> ReadRowArray()
            {
                var list = new List<AnalysisRow>();
                Expect('[');
                SkipWs();
                while (Peek() != ']')
                {
                    list.Add(ReadRow());
                    SkipWs();
                    if (Peek() == ',') { _pos++; SkipWs(); }
                }
                Expect(']');
                return list;
            }

            private AnalysisRow ReadRow()
            {
                if (TryNull()) return null;

                var r = new AnalysisRow();
                Expect('{');
                SkipWs();
                while (Peek() != '}')
                {
                    var key = ReadString();
                    SkipWs(); Expect(':'); SkipWs();

                    if (string.Equals(key, "RowType", StringComparison.OrdinalIgnoreCase)) r.RowType = ReadNullableString();
                    else if (string.Equals(key, "LoadCombination", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(key, "Load", StringComparison.OrdinalIgnoreCase)) r.LoadCombination = ReadNullableString();
                    else if (string.Equals(key, "Element", StringComparison.OrdinalIgnoreCase)) r.Element = ReadNullableInt();
                    else if (string.Equals(key, "N", StringComparison.OrdinalIgnoreCase)) r.N = ReadNullableDouble();
                    else if (string.Equals(key, "Nt", StringComparison.OrdinalIgnoreCase)) r.Nt = ReadNullableDouble();
                    else if (string.Equals(key, "Nc", StringComparison.OrdinalIgnoreCase)) r.Nc = ReadNullableDouble();
                    else if (string.Equals(key, "Qy", StringComparison.OrdinalIgnoreCase)) r.Qy = ReadNullableDouble();
                    else if (string.Equals(key, "Qz", StringComparison.OrdinalIgnoreCase)) r.Qz = ReadNullableDouble();
                    else if (string.Equals(key, "Mx", StringComparison.OrdinalIgnoreCase)) r.Mx = ReadNullableDouble();
                    else if (string.Equals(key, "My", StringComparison.OrdinalIgnoreCase)) r.My = ReadNullableDouble();
                    else if (string.Equals(key, "Mz", StringComparison.OrdinalIgnoreCase)) r.Mz = ReadNullableDouble();
                    else if (string.Equals(key, "Mw", StringComparison.OrdinalIgnoreCase)) r.Mw = ReadNullableDouble();
                    else if (string.Equals(key, "U", StringComparison.OrdinalIgnoreCase)) r.U = ReadNullableDouble();
                    else if (string.Equals(key, "Psi", StringComparison.OrdinalIgnoreCase)) r.Psi = ReadNullableDouble();
                    else SkipValue();

                    SkipWs();
                    if (Peek() == ',') { _pos++; SkipWs(); }
                }
                Expect('}');
                return r;
            }

            // ?? Примитивы ??

            private bool TryNull()
            {
                SkipWs();
                if (_pos + 3 < _s.Length
                    && _s[_pos] == 'n' && _s[_pos + 1] == 'u' && _s[_pos + 2] == 'l' && _s[_pos + 3] == 'l')
                {
                    _pos += 4;
                    return true;
                }
                return false;
            }

            private double? ReadNullableDouble()
            {
                SkipWs();
                if (TryNull()) return null;

                int start = _pos;
                while (_pos < _s.Length && IsNumberChar(_s[_pos])) _pos++;
                var raw = _s.Substring(start, _pos - start);
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return d;
                return null;
            }

            private int? ReadNullableInt()
            {
                SkipWs();
                if (TryNull()) return null;

                int start = _pos;
                while (_pos < _s.Length && IsNumberChar(_s[_pos])) _pos++;
                var raw = _s.Substring(start, _pos - start);
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    return n;
                return null;
            }

            private string ReadNullableString()
            {
                SkipWs();
                if (TryNull()) return null;
                return ReadString();
            }

            private string ReadString()
            {
                SkipWs();
                Expect('"');
                var sb = new StringBuilder();
                while (_pos < _s.Length)
                {
                    var c = _s[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\' && _pos < _s.Length)
                    {
                        var next = _s[_pos++];
                        switch (next)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            default: sb.Append(next); break;
                        }
                        continue;
                    }
                    sb.Append(c);
                }
                return sb.ToString();
            }

            private void SkipValue()
            {
                SkipWs();
                var c = Peek();
                if (c == '"') { ReadString(); return; }
                if (c == '{') { SkipBlock('{', '}'); return; }
                if (c == '[') { SkipBlock('[', ']'); return; }
                // number, bool, null
                while (_pos < _s.Length && _s[_pos] != ',' && _s[_pos] != '}' && _s[_pos] != ']')
                    _pos++;
            }

            private void SkipBlock(char open, char close)
            {
                Expect(open);
                int depth = 1;
                bool inStr = false;
                while (_pos < _s.Length && depth > 0)
                {
                    var c = _s[_pos++];
                    if (inStr) { if (c == '\\') _pos++; else if (c == '"') inStr = false; continue; }
                    if (c == '"') { inStr = true; continue; }
                    if (c == open) depth++;
                    if (c == close) depth--;
                }
            }

            private void SkipWs()
            {
                while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++;
            }

            private char Peek() => _pos < _s.Length ? _s[_pos] : '\0';

            private void Expect(char c)
            {
                SkipWs();
                if (_pos >= _s.Length || _s[_pos] != c)
                    throw new FormatException($"Expected '{c}' at position {_pos}");
                _pos++;
            }

            private static bool IsNumberChar(char c) =>
                char.IsDigit(c) || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E';
        }
    }
}
