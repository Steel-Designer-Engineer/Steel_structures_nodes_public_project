using System;
using System.Collections.Generic;
using System.Linq;
using Steel_structures_nodes_public_project.Wpf.Models;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// Минимальный JSON-парсер для чтения списков узлов и таблиц взаимодействия.
    /// </summary>
    public sealed class SimpleJsonParser
    {
        private readonly Dictionary<string, object> _root;

        public SimpleJsonParser(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            // Some assets are saved with UTF-8 BOM. Trim it once globally.
            if (json.Length > 0 && json[0] == '\uFEFF')
                json = json.TrimStart('\uFEFF');

            var p = new Parser(json);
            p.SkipWs();

            // Accept either an object root or a flat array root (interaction_tables.json)
            if (p.Peek() == '[')
            {
                _root = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["__array"] = p.ReadArray()
                };
            }
            else
            {
                _root = p.ReadObjectInternal();
            }
        }

        public string TryFindInteractionConnectionName(string name, string beam, string column, string connectionName)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(beam) || string.IsNullOrWhiteSpace(connectionName))
                return null;

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list))
                return null;

            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0)
                    continue;

                var b = ResolveProfileBeam(d);
                if (b == null)
                    continue;
                var c = ResolveProfileColumn(d);
                if (c == null)
                    continue;

                if (!TryGetStringLoose(d, "CONNECTION_CODE", out var cn))
                    continue;

                if (!string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(b.Trim(), beam.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(cn?.Trim(), connectionName.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                return cn.Trim();
            }

            return null;
        }
        
        /// <summary>
        /// Пытается найти имя соединения взаимодействия, связанное с указанным именем профиля и балкой.
        /// </summary>
        /// <remarks>Этот метод выполняет поиск в коллекции записей профиля и балки, чтобы найти соответствующий
        /// код соединения. Оба параметра сравниваются без учета регистра после удаления пробелов. Если
        /// соответствующая запись не найдена, метод возвращает null.</remarks>
        /// <param name="name">Имя профиля для сопоставления с записями в источнике данных. Этот параметр не может быть null или состоять только из пробелов.</param>
        /// <param name="beam">Идентификатор балки для сопоставления с записями в источнике данных. Этот параметр не может быть null или состоять только из пробелов.</param>
        /// <returns>Имя соединения, соответствующее указанному профилю и балке, если совпадение найдено; в противном случае null.</returns>
        public string TryFindInteractionConnectionName(string name, string beam)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(beam))
                return null;

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list))
                return null;

            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0)
                    continue;

                var b = ResolveProfileBeam(d);
                if (b == null)
                    continue;

                if (!string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(b.Trim(), beam.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryGetStringLoose(d, "CONNECTION_CODE", out var cn) && !string.IsNullOrWhiteSpace(cn))
                    return cn.Trim();
            }

            return null;
        }

        public string[] TryReadNestedStringArraysFlattened(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return null;
            if (!_root.TryGetValue(propertyName, out var v)) return null;

            // Accept either { key: ["a","b"], key2: [...] } or [ ["a"], ["b"] ]
            if (v is Dictionary<string, object> map)
            {
                return map.Values
                    .OfType<List<object>>()
                    .SelectMany(l => l.OfType<string>())
                    .ToArray();
            }

            if (v is List<object> outer)
            {
                return outer
                    .OfType<List<object>>()
                    .SelectMany(l => l.OfType<string>())
                    .ToArray();
            }

            return null;
        }

        public ConnectionOptionViewModel[] TryReadNodeList()
        {
            // Expected shape: { "items": [ { "code": "H1_04", "description": "..." }, ... ] }
            if (!_root.TryGetValue("items", out var v) || !(v is List<object> list))
                return Array.Empty<ConnectionOptionViewModel>();

            var result = new List<ConnectionOptionViewModel>();
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                if (!TryGetString(d, "code", out var code) || string.IsNullOrWhiteSpace(code))
                    continue;

                TryGetString(d, "description", out var desc);

                result.Add(new ConnectionOptionViewModel
                {
                    Code = code,
                    Description = desc ?? string.Empty,
                });
            }

            return result
                .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public Dictionary<string, object> TryFindInteractionRow(string name, string connectionName)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(connectionName))
                return null;

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list))
                return null;

            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0)
                    continue;

                if (!TryGetStringLoose(d, "CONNECTION_CODE", out var cn))
                    continue;

                if (!string.Equals(n, name.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(cn?.Trim(), connectionName.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                return d;
            }

            return null;
        }

        public string TryGetString(Dictionary<string, object> row, string key)
        {
            if (row == null || string.IsNullOrWhiteSpace(key))
                return null;
            TryGetStringLoose(row, key, out var val);
            return val;
        }

        /// <summary>
        /// Resolves a string value from a nested path (e.g. "Geometry.Beam.ProfileBeam").
        /// Falls back to flat key lookup if path has no dots.
        /// </summary>
        public string TryGetNestedString(Dictionary<string, object> row, string path)
        {
            if (row == null || string.IsNullOrWhiteSpace(path))
                return null;
            var resolved = ResolveNestedValue(row, path);
            if (resolved == null) return null;
            if (resolved is string s) return s;
            return resolved.ToString();
        }

        /// <summary>
        /// Resolves a nullable double from a nested path (e.g. "InternalForces.N").
        /// Falls back to flat key lookup if path has no dots.
        /// </summary>
        public double? TryGetNestedDouble(Dictionary<string, object> row, string path)
        {
            if (row == null || string.IsNullOrWhiteSpace(path))
                return null;
            var resolved = ResolveNestedValue(row, path);
            return ConvertToNullableDouble(resolved);
        }

        public double? TryGetNullableDouble(Dictionary<string, object> row, string key)
        {
            if (row == null || string.IsNullOrWhiteSpace(key))
                return null;

            if (!row.TryGetValue(key, out var v) || v == null)
                return null;

            return ConvertToNullableDouble(v);
        }

        private static double? ConvertToNullableDouble(object v)
        {
            if (v == null) return null;

            if (v is double dd)
                return dd;

            var s = v as string;
            if (s == null)
                s = v.ToString();

            s = (s ?? string.Empty).Trim();
            if (s.Length == 0) return null;
            if (s == "?" || s == "-" || s.Equals("N/A", StringComparison.OrdinalIgnoreCase)) return null;

            s = s.Replace(',', '.');
            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dval))
                return dval;

            return null;
        }

        /// <summary>
        /// Navigates into nested Dictionary objects using a dot-separated path.
        /// E.g. "Geometry.Beam.ProfileBeam" walks root["Geometry"]["Beam"]["ProfileBeam"].
        /// </summary>
        private static object ResolveNestedValue(Dictionary<string, object> root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path))
                return null;

            var parts = path.Split('.');
            object current = root;

            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out var next) || next == null)
                        return null;
                    current = next;
                }
                else
                {
                    return null;
                }
            }

            return current;
        }

        private static string ResolveRowName(Dictionary<string, object> d)
        {
            // interaction_tables.json uses "Name" (PascalCase), but some sources may use "NAME".
            // Support both to keep combobox values consistent with the JSON.
            TryGetStringLoose(d, "Name", out var n);
            if (string.IsNullOrWhiteSpace(n))
                TryGetStringLoose(d, "NAME", out n);
            n = (n ?? string.Empty).Trim();
            if (n.Length > 0)
                return n;

            // Derive Name from CONNECTION_CODE prefix (e.g. "H12_27" -> "H12")
            if (TryGetStringLoose(d, "CONNECTION_CODE", out var code))
            {
                code = (code ?? string.Empty).Trim();
                var idx = code.LastIndexOf('_');
                if (idx > 0)
                    return code.Substring(0, idx);
            }

            return string.Empty;
        }

        private static bool TryGetString(Dictionary<string, object> d, string name, out string value)
        {
            value = null;
            if (d != null && d.TryGetValue(name, out var v) && v is string s)
            {
                value = s;
                return true;
            }
            return false;
        }

        private static bool TryGetStringLoose(Dictionary<string, object> d, string name, out string value)
        {
            value = null;
            if (d == null || string.IsNullOrWhiteSpace(name)) return false;
            if (!d.TryGetValue(name, out var v) || v == null) return false;

            if (v is string s)
            {
                value = s;
                return true;
            }

            value = v.ToString();
            return true;
        }

        /// <summary>
        /// Resolves ProfileBeam from nested Geometry.Beam.ProfileBeam or flat ProfileBeam.
        /// </summary>
        private static string ResolveProfileBeam(Dictionary<string, object> d)
        {
            // Nested: Geometry.Beam.ProfileBeam
            var val = ResolveNestedValue(d, "Geometry.Beam.ProfileBeam");
            if (val != null)
            {
                var s = val is string str ? str : val.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            // Flat fallback
            if (TryGetStringLoose(d, "ProfileBeam", out var flat) && !string.IsNullOrWhiteSpace(flat))
                return flat;
            return null;
        }

        /// <summary>
        /// Resolves ProfileColumn from nested Geometry.Column.ProfileColumn or flat ProfileColumn.
        /// </summary>
        private static string ResolveProfileColumn(Dictionary<string, object> d)
        {
            // Nested: Geometry.Column.ProfileColumn
            var val = ResolveNestedValue(d, "Geometry.Column.ProfileColumn");
            if (val != null)
            {
                var s = val is string str ? str : val.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            // Flat fallback
            if (TryGetStringLoose(d, "ProfileColumn", out var flat) && !string.IsNullOrWhiteSpace(flat))
                return flat;
            return null;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; }

            public char Peek() => _i < _s.Length ? _s[_i] : '\0';

            public void SkipWs()
            {
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n') { _i++; continue; }
                    break;
                }
            }

            private bool TryConsume(char c)
            {
                if (_i < _s.Length && _s[_i] == c) { _i++; return true; }
                return false;
            }

            private void Expect(char c)
            {
                if (!TryConsume(c))
                    throw new FormatException("Expected '" + c + "' at " + _i);
            }

            public Dictionary<string, object> ReadObjectInternal()
            {
                SkipWs();
                Expect('{');
                SkipWs();

                var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (TryConsume('}'))
                    return map;

                while (true)
                {
                    SkipWs();
                    var name = ReadString();
                    SkipWs();
                    Expect(':');
                    SkipWs();
                    map[name] = ReadValue();
                    SkipWs();
                    if (TryConsume(',')) continue;
                    Expect('}');
                    return map;
                }
            }

            internal List<object> ReadArray()
            {
                Expect('[');
                SkipWs();
                var list = new List<object>();
                if (TryConsume(']')) return list;

                while (true)
                {
                    SkipWs();
                    list.Add(ReadValue());
                    SkipWs();
                    if (TryConsume(',')) continue;
                    Expect(']');
                    return list;
                }
            }

            private object ReadValue()
            {
                SkipWs();
                if (_i >= _s.Length) throw new FormatException("Unexpected end");

                var c = _s[_i];
                if (c == '"') return ReadString();
                if (c == '{') return ReadObjectInternal();
                if (c == '[') return ReadArray();
                if (TryReadNull()) return null;
                return ReadNumber();
            }

            private string ReadString()
            {
                Expect('"');
                var sb = new System.Text.StringBuilder();

                while (_i < _s.Length)
                {
                    var c = _s[_i++];
                    if (c == '"')
                        return sb.ToString();
                    if (c == '\\')
                    {
                        if (_i >= _s.Length) throw new FormatException("Bad escape");
                        var e = _s[_i++];
                        switch (e)
                        {
                            case '\\': sb.Append('\\'); break;
                            case '"': sb.Append('"'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            default: sb.Append(e); break;
                        }
                        continue;
                    }
                    sb.Append(c);
                }
                throw new FormatException("Unterminated string");
            }

            private bool TryReadNull()
            {
                if (_i + 3 < _s.Length && _s[_i] == 'n' && _s[_i + 1] == 'u' && _s[_i + 2] == 'l' && _s[_i + 3] == 'l')
                {
                    _i += 4;
                    return true;
                }
                return false;
            }

            private double ReadNumber()
            {
                var start = _i;
                while (_i < _s.Length)
                {
                    var c = _s[_i];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') { _i++; continue; }
                    break;
                }

                var token = _s.Substring(start, _i - start);
                if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
                    return v;

                throw new FormatException("Invalid number: " + token);
            }
        }

        public string[] TryReadConnectionCodesFromArray()
        {
            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list) || list.Count == 0)
                return Array.Empty<string>();

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                if (TryGetStringLoose(d, "CONNECTION_CODE", out var code) && !string.IsNullOrWhiteSpace(code))
                    set.Add(code.Trim());
            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public string[] TryReadDistinctStringValuesFromArray(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Array.Empty<string>();

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list) || list.Count == 0)
                return Array.Empty<string>();

            var isNameKey = string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase);

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                string s;
                if (isNameKey)
                {
                    s = ResolveRowName(d);
                }
                else
                {
                    if (!d.TryGetValue(key, out var vv) || vv == null)
                        continue;
                    s = vv as string;
                    if (s == null)
                        s = vv.ToString();
                    s = (s ?? string.Empty).Trim();
                }

                if (s.Length == 0)
                    continue;

                set.Add(s);
            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        /// <summary>
        /// Возвращает все строки из JSON-массива за один проход: (Name, CONNECTION_CODE, raw dictionary).
        /// Используется тестами для избежания O(N²) повторных вызовов TryFindInteractionRow.
        /// </summary>
        public List<(string Name, string Code, Dictionary<string, object> Raw)> TryReadAllInteractionRows()
        {
            var result = new List<(string, string, Dictionary<string, object>)>();

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list))
                return result;

            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0) continue;

                if (!TryGetStringLoose(d, "CONNECTION_CODE", out var cn) || string.IsNullOrWhiteSpace(cn))
                    continue;

                result.Add((n, cn.Trim(), d));
            }

            return result;
        }

        public string[] TryReadDistinctProfileBeamsByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0)
                return Array.Empty<string>();

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list) || list.Count == 0)
                return Array.Empty<string>();

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0 || !string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var profile = ResolveProfileBeam(d);
                if (!string.IsNullOrWhiteSpace(profile))
                    set.Add(profile.Trim());
            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public string[] TryReadDistinctProfileColumnsByName(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0)
                return Array.Empty<string>();

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list) || list.Count == 0)
                return Array.Empty<string>();

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0 || !string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var profile = ResolveProfileColumn(d);
                if (!string.IsNullOrWhiteSpace(profile))
                    set.Add(profile.Trim());
            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public string[] TryReadDistinctConnectionNames(string name, string beam)
        {
            name = (name ?? string.Empty).Trim();
            beam = (beam ?? string.Empty).Trim();
            if (name.Length == 0 || beam.Length == 0)
                return Array.Empty<string>();

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list) || list.Count == 0)
                return Array.Empty<string>();

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0 || !string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var b = ResolveProfileBeam(d);
                if (b == null || !string.Equals(b.Trim(), beam, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryGetStringLoose(d, "CONNECTION_CODE", out var cn) && !string.IsNullOrWhiteSpace(cn))
                    set.Add(cn.Trim());
            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public string[] TryReadDistinctConnectionNames(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length == 0)
                return Array.Empty<string>();

            if (!_root.TryGetValue("__array", out var v) || !(v is List<object> list) || list.Count == 0)
                return Array.Empty<string>();

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in list)
            {
                if (!(item is Dictionary<string, object> d))
                    continue;

                var n = ResolveRowName(d);
                if (n.Length == 0 || !string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryGetStringLoose(d, "CONNECTION_CODE", out var cn) && !string.IsNullOrWhiteSpace(cn))
                    set.Add(cn.Trim());
            }

            return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
