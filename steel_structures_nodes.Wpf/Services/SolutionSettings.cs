using System;
using System.Globalization;
using System.IO;

namespace Steel_structures_nodes_public_project.Wpf.Services
{
    /// <summary>
    /// Глобальные настройки решения, сохраняемые между сеансами.
    /// Файл settings.json хранится рядом с результатами расчёта.
    /// </summary>
    internal static class SolutionSettings
    {
        private const string FileName = "solution_settings.json";

        /// <summary>Загружает ?f из файла настроек. Если файл отсутствует — возвращает "1".</summary>
        public static string LoadGammaF()
        {
            var path = GetSettingsPath();
            if (path == null || !File.Exists(path))
                return "1";

            try
            {
                var json = File.ReadAllText(path);
                var value = ExtractJsonValue(json, "gammaF");
                return string.IsNullOrWhiteSpace(value) ? "1" : value;
            }
            catch
            {
                return "1";
            }
        }

        /// <summary>Сохраняет ?f в файл настроек.</summary>
        public static void SaveGammaF(string value)
        {
            var path = GetSettingsPath();
            if (path == null) return;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = $"{{\n  \"gammaF\": \"{(value ?? "1").Trim()}\"\n}}";
                File.WriteAllText(path, json);
            }
            catch
            {
                // Не критично — настройки не сохранятся
            }
        }

        private static string GetSettingsPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 10 && dir != null; i++)
            {
                var projectDir = Path.Combine(dir.FullName, "steel_structures_nodes.Calculate");
                if (Directory.Exists(projectDir))
                {
                    var resultDir = Path.Combine(projectDir, "ResultCalculate");
                    if (!Directory.Exists(resultDir))
                        Directory.CreateDirectory(resultDir);
                    return Path.Combine(resultDir, FileName);
                }
                dir = dir.Parent;
            }

            // Fallback: рядом с exe
            var fallback = Path.Combine(baseDir, "ResultCalculate");
            if (!Directory.Exists(fallback))
                Directory.CreateDirectory(fallback);
            return Path.Combine(fallback, FileName);
        }

        private static string ExtractJsonValue(string json, string key)
        {
            var search = $"\"{key}\"";
            var idx = json.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx = json.IndexOf(':', idx + search.Length);
            if (idx < 0) return null;

            var rest = json.Substring(idx + 1).Trim();
            if (rest.StartsWith("\""))
            {
                var end = rest.IndexOf('"', 1);
                return end > 0 ? rest.Substring(1, end - 1) : null;
            }

            // Числовое значение без кавычек
            var sb = new System.Text.StringBuilder();
            foreach (var c in rest)
            {
                if (char.IsDigit(c) || c == '.' || c == ',' || c == '-') sb.Append(c);
                else if (sb.Length > 0) break;
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}
