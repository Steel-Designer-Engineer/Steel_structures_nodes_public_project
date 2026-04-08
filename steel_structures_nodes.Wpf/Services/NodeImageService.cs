using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// Сервис загрузки изображений узлов по коду (ищет в ресурсах приложения и файловой системе).
    /// </summary>
    internal static class NodeImageService
    {
        /// <summary>
        /// Загружает все изображения для указанного кода узла (например, BH.png, BH_1.png, BH_2.png …).
        /// </summary>
        public static List<ImageSource> LoadAllNodeImages(string nodeCode)
        {
            var result = new List<ImageSource>();
            if (string.IsNullOrWhiteSpace(nodeCode))
                return result;

            try
            {
                var candidates = BuildImageCodeCandidates(nodeCode);
                var exts = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

                // Collect from filesystem
                var dirs = GetSearchDirectories();
                var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var nodeDir in dirs)
                {
                    if (!Directory.Exists(nodeDir))
                        continue;

                    foreach (var c in candidates)
                    {
                        // Exact match: e.g. BH.png
                        foreach (var ext in exts)
                        {
                            var p = Path.Combine(nodeDir, c + ext);
                            if (File.Exists(p) && addedPaths.Add(p))
                            {
                                var img = LoadPreviewImage(p);
                                if (img != null) result.Add(img);
                            }
                        }

                        // Numbered variants: e.g. BH_1.png, BH_2.png, …
                        for (int i = 1; i <= 50; i++)
                        {
                            bool found = false;
                            foreach (var ext in exts)
                            {
                                var p = Path.Combine(nodeDir, c + "_" + i + ext);
                                if (File.Exists(p) && addedPaths.Add(p))
                                {
                                    var img = LoadPreviewImage(p);
                                    if (img != null) result.Add(img);
                                    found = true;
                                    break;
                                }
                            }
                            if (!found) break;
                        }
                    }

                    if (result.Count > 0)
                        break; // found in this directory, stop searching others
                }

                // Fallback: try pack resources for the first image
                if (result.Count == 0)
                {
                    foreach (var c in candidates)
                    {
                        try
                        {
                            var pack = "pack://application:,,,/nodeImage/" + Uri.EscapeDataString(c) + ".png";
                            var bi = new BitmapImage();
                            bi.BeginInit();
                            bi.UriSource = new Uri(pack, UriKind.Absolute);
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                            bi.EndInit();
                            bi.Freeze();
                            result.Add(bi);
                            break;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return result;
        }

        public static ImageSource TryLoadNodeImage(string nodeCode)
        {
            var all = LoadAllNodeImages(nodeCode);
            return all.Count > 0 ? all[0] : null;
        }

        private static List<string> GetSearchDirectories()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dirsToTry = new List<string>
            {
                Path.Combine(baseDir, "nodeImage"),
                Path.Combine(baseDir, "Assets", "nodeImage")
            };

            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                dirsToTry.Add(Path.Combine(dir.FullName, "steel_structures_nodes.Wpf", "nodeImage"));
                dir = dir.Parent;
            }

            return dirsToTry.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string[] BuildImageCodeCandidates(string nodeCode)
        {
            var c0 = (nodeCode ?? string.Empty).Trim();
            if (c0.Length == 0)
                return Array.Empty<string>();

            var list = new List<string>();

            void Add(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                s = s.Trim();
                if (s.Length == 0) return;
                if (!list.Contains(s, StringComparer.OrdinalIgnoreCase))
                    list.Add(s);
            }

            Add(c0);
            Add(ExtractPrefix(c0));

            var dash = c0.IndexOf('-');
            if (dash > 0)
                Add(c0.Substring(0, dash));

            foreach (var s in list.ToArray())
            {
                if (s.EndsWith("_", StringComparison.Ordinal))
                    Add(s.TrimEnd('_'));
                else
                    Add(s + "_");
            }

            return list.ToArray();
        }

        private static string ExtractPrefix(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            var t = code.Trim();
            var i = t.IndexOf('_');
            return i > 0 ? t.Substring(0, i) : t;
        }

        private static ImageSource LoadPreviewImage(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
                return null;

            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(absolutePath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch
            {
                return null;
            }
        }
    }
}
