using System;
using System.IO;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// РЈС‚РёР»РёС‚Р° РґР»СЏ РїРѕРёСЃРєР° JSON-С„Р°Р№Р»РѕРІ СЂРµСЃСѓСЂСЃРѕРІ WPF-РїСЂРѕРµРєС‚Р° РІ РєР°С‚Р°Р»РѕРіР°С… СЃР±РѕСЂРєРё Рё СЂРµРїРѕР·РёС‚РѕСЂРёСЏ.
    /// </summary>
    internal static class JsonAssetPathResolver
    {
        public static string ResolveWpfAssetsJsonPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "steel_structures_nodes.Wpf", "Assets", fileName);
                if (File.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            return null;
        }

        public static string ResolveRs1ListsJsonPath()
        {
            var path = ResolveWpfAssetsJsonPath("rs1_lists.json");
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return path;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var nearExe = Path.Combine(baseDir, "rs1_lists.json");
            if (File.Exists(nearExe))
                return nearExe;

            return null;
        }
    }
}
