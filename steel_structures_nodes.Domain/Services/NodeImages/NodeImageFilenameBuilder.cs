using System;
using System.Collections.Generic;
using System.Linq;

namespace steel_structures_nodes.Domain.Services.NodeImages;

/// <summary>
/// Генерирует возможные имена файлов изображений узлов по коду соединения.
/// </summary>
public static class NodeImageFilenameBuilder
{
    private static readonly string[] Extensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];
    private const int MaxNumberedVariants = 20;

    public static IReadOnlyList<string> BuildAllPossibleFilenames(string nodeCode)
    {
        var candidates = BuildCodeCandidates(nodeCode);
        var list = new List<string>();

        foreach (var candidate in candidates)
        {
            foreach (var extension in Extensions)
                list.Add(candidate + extension);

            for (int i = 1; i <= MaxNumberedVariants; i++)
            {
                foreach (var extension in Extensions)
                    list.Add($"{candidate}_{i}{extension}");
            }
        }

        return list;
    }

    private static string[] BuildCodeCandidates(string nodeCode)
    {
        var normalizedCode = (nodeCode ?? string.Empty).Trim();
        if (normalizedCode.Length == 0)
            return [];

        var candidates = new List<string>();

        void Add(string value)
        {
            var candidate = value.Trim();
            if (candidate.Length > 0 && !candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                candidates.Add(candidate);
        }

        Add(normalizedCode);
        Add(ExtractPrefix(normalizedCode));

        var dashIndex = normalizedCode.IndexOf('-');
        if (dashIndex > 0)
            Add(normalizedCode[..dashIndex]);

        foreach (var candidate in candidates.ToArray())
        {
            if (candidate.EndsWith('_'))
                Add(candidate.TrimEnd('_'));
            else
                Add(candidate + "_");
        }

        return [.. candidates];
    }

    private static string ExtractPrefix(string code)
    {
        var trimmed = code.Trim();
        var separatorIndex = trimmed.IndexOf('_');
        return separatorIndex > 0 ? trimmed[..separatorIndex] : trimmed;
    }
}
