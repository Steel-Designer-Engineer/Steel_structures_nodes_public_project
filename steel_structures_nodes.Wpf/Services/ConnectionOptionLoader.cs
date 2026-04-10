using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using steel_structures_nodes.Wpf.Models;

namespace steel_structures_nodes.Wpf.Services
{
    /// <summary>
    /// Р—Р°РіСЂСѓР·С‡РёРє РІР°СЂРёР°РЅС‚РѕРІ СѓР·Р»РѕРІС‹С… СЃРѕРµРґРёРЅРµРЅРёР№ РёР· JSON-С„Р°Р№Р»РѕРІ СЂРµСЃСѓСЂСЃРѕРІ.
    /// </summary>
    internal static class ConnectionOptionLoader
    {
        public static ConnectionOptionViewModel[] LoadAll()
        {
            var list = new List<ConnectionOptionViewModel>();

            var fromCodeNodes = TryLoadCodeNodesFromJson();
            if (fromCodeNodes != null && fromCodeNodes.Length > 0)
                list.AddRange(fromCodeNodes);

            var fromJson = TryLoadNodeTopologiesFromJson();
            if (fromJson != null && fromJson.Length > 0)
                list.AddRange(fromJson);

            return list
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var best = g.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Description)) ?? g.First();
                    best.Code = best.Code.Trim();
                    best.Description = best.Description ?? string.Empty;
                    return best;
                })
                .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static ConnectionOptionViewModel[] TryLoadCodeNodesFromJson()
        {
            try
            {
                var path = JsonAssetPathResolver.ResolveWpfAssetsJsonPath("codeNodes.json");
                if (path == null || !File.Exists(path))
                    return null;

                var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var p = new SimpleJsonParser(json);

                var codes = p.TryReadConnectionCodesFromArray();
                if (codes == null || codes.Length == 0)
                    codes = p.TryReadNestedStringArraysFlattened("groups");

                if (codes == null || codes.Length == 0)
                    return null;

                return codes
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .Select(code => new ConnectionOptionViewModel { Code = code, Description = code })
                    .ToArray();
            }
            catch
            {
                return null;
            }
        }

        private static ConnectionOptionViewModel[] TryLoadNodeTopologiesFromJson()
        {
            try
            {
                var path = JsonAssetPathResolver.ResolveWpfAssetsJsonPath("node_topologies.json");
                if (path == null || !File.Exists(path))
                    return null;

                var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                var p = new SimpleJsonParser(json);
                return p.TryReadNodeList();
            }
            catch
            {
                return null;
            }
        }
    }
}
