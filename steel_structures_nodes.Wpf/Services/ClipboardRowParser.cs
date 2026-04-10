using System;
using System.Collections.Generic;
using System.Linq;
using steel_structures_nodes.Calculate.Models.RSN;

namespace steel_structures_nodes.Wpf.Services
{
    /// <summary>
    /// РџР°СЂСЃРµСЂ СЃС‚СЂРѕРє Р РЎРЈ/Р РЎРќ РёР· С‚РµРєСЃС‚Р° Р±СѓС„РµСЂР° РѕР±РјРµРЅР° (TAB-СЂР°Р·РґРµР»С‘РЅРЅС‹Рµ Р·РЅР°С‡РµРЅРёСЏ).
    /// </summary>
    internal static class ClipboardRowParser
    {
        public static IEnumerable<RsnRow> ParseRows(string text)
        {
            var normalized = (text ?? string.Empty).Replace("\r\n", "\n");
            var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l));

            foreach (var line in lines)
            {
                if (IsHeaderLine(line))
                    continue;

                var parts = line.Split(new[] { '\t' }, StringSplitOptions.None)
                    .Select(p => (p ?? string.Empty).Trim())
                    .ToArray();

                if (parts.Length < 11)
                    throw new InvalidOperationException("Row must have 11 TAB-separated columns");

                yield return new RsnRow
                {
                    DclNo = parts[0],
                    ElemType = parts[1],
                    Elem = parts[2],
                    Sect = parts[3],
                    N = parts[4],
                    Mx = parts[5],
                    My = parts[6],
                    Qz = parts[7],
                    Mz = parts[8],
                    Qy = parts[9],
                    Mw = parts[10],
                };
            }
        }

        private static bool IsHeaderLine(string line)
        {
            var l = (line ?? string.Empty).Trim();
            return l.StartsWith("DCL No", StringComparison.OrdinalIgnoreCase);
        }
    }
}
