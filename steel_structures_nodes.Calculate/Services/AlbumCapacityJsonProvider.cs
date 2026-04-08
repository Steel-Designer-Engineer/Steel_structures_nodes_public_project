using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Steel_structures_nodes_public_project.Calculate.Models;

namespace Steel_structures_nodes_public_project.Calculate.Services
{
    /// <summary>
    /// Провайдер несущей способности узлов, загружающий данные из JSON-файла альбома.
    /// </summary>
    public sealed class AlbumCapacityJsonProvider : IAlbumCapacityProvider
    {
        private readonly IReadOnlyDictionary<string, ForceRow> _rows;

        public AlbumCapacityJsonProvider(string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath)) throw new ArgumentNullException(nameof(jsonFilePath));
            if (!File.Exists(jsonFilePath)) throw new FileNotFoundException("Album capacity json not found", jsonFilePath);

            var json = File.ReadAllText(jsonFilePath);
            _rows = Parse(json);
        }

        public ForceRow GetByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            _rows.TryGetValue(key.Trim(), out var row);
            return row;
        }

        private static IReadOnlyDictionary<string, ForceRow> Parse(string json)
        {
            var rows = new Dictionary<string, ForceRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in SimpleJson.ParseObject(json).GetArray("rows"))
            {
                var row = new ForceRow
                {
                    Key = r.GetString("key"),
                    AlbumN = r.GetDouble("AlbumN"),
                    AlbumNt = r.GetDouble("AlbumNt"),
                    AlbumNc = r.GetDouble("AlbumNc"),
                    AlbumQy = r.GetDouble("AlbumQy"),
                    AlbumQz = r.GetDouble("AlbumQz"),
                    AlbumMx = r.GetDouble("AlbumMx"),
                    AlbumMy = r.GetDouble("AlbumMy"),
                    AlbumMz = r.GetDouble("AlbumMz"),
                    AlbumMw = r.GetDouble("AlbumMw"),
                    AlbumT = r.GetDouble("AlbumT"),
                    AlbumPsi = r.GetDouble("AlbumPsi"),
                };

                if (!string.IsNullOrWhiteSpace(row.Key))
                    rows[row.Key.Trim()] = row;
            }

            return rows;
        }

        /// <summary>
        /// Минимальный JSON-парсер для чтения альбома несущей способности.
        /// </summary>
        private sealed class SimpleJson
        {
            public static JsonObject ParseObject(string json)
            {
                if (json == null) throw new ArgumentNullException(nameof(json));
                var p = new Parser(json);
                p.SkipWs();
                return new JsonObject(p.ReadObjectInternal());
            }

            internal sealed class JsonObject
            {
                private readonly Dictionary<string, object> _m;

                public JsonObject(Dictionary<string, object> m)
                {
                    _m = m;
                }

                public IEnumerable<JsonObject> GetArray(string name)
                {
                    if (!_m.TryGetValue(name, out var v) || !(v is List<object> list))
                        return Enumerable.Empty<JsonObject>();

                    return list.OfType<Dictionary<string, object>>().Select(d => new JsonObject(d));
                }

                public string GetString(string name)
                {
                    if (_m.TryGetValue(name, out var v) && v is string s) return s;
                    return null;
                }

                public double GetDouble(string name)
                {
                    if (_m.TryGetValue(name, out var v))
                    {
                        if (v is double d) return d;
                        if (v is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
                    }
                    return 0d;
                }
            }

            private sealed class Parser
            {
                private readonly string _s;
                private int _i;

                public Parser(string s) { _s = s; }

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

                private List<object> ReadArray()
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

                private string ReadString()
                {
                    Expect('"');
                    var start = _i;
                    var sb = new System.Text.StringBuilder();

                    while (_i < _s.Length)
                    {
                        var c = _s[_i++];
                        if (c == '"')
                            return sb.Length == 0 ? _s.Substring(start, _i - start - 1) : sb.ToString();
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
                        if (sb.Length == 0 && c != '"')
                        {
                            // Lazily start buffering only if needed
                            sb.Append(_s.Substring(start, _i - start - 1));
                        }
                        if (sb.Length > 0) sb.Append(c);
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
                    if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                        return v;

                    throw new FormatException("Invalid number: " + token);
                }
            }
        }
    }
}
