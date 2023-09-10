﻿using GIMI_ModManager.Core.Entities;

namespace GIMI_ModManager.Core.Helpers;

// This class just holds code that i don't know where to put yet.
public static class IniConfigHelpers
{
    public static SkinModKeySwap? ParseKeySwap(ICollection<string> fileLines, string sectionLine)
    {
        var skinModKeySwap = new SkinModKeySwap
        {
            SectionKey = sectionLine.Trim()
        };

        foreach (var line in fileLines)
        {
            if (IsIniKey(line, SkinModKeySwap.ForwardIniKey))
                skinModKeySwap.ForwardHotkey = GetIniValue(line);

            else if (IsIniKey(line, SkinModKeySwap.BackwardIniKey))
                skinModKeySwap.BackwardHotkey = GetIniValue(line);

            else if (IsIniKey(line, SkinModKeySwap.TypeIniKey))
                skinModKeySwap.Type = GetIniValue(line);

            else if (IsIniKey(line, SkinModKeySwap.SwapVarIniKey))
                skinModKeySwap.SwapVar = GetIniValue(line)?.Split(',');

            else if (IsSection(line))
                break;
        }

        var result = skinModKeySwap.AnyValues() ? skinModKeySwap : null;
        return result;
    }

    public static string? GetIniValue(string line)
    {
        if (IsComment(line)) return null;

        var split = line.Split('=');

        if (split.Length <= 2) return split.Length != 2 ? null : split[1].Trim();


        split[1] = string.Join("", split.Skip(1));
        return split[1].Trim();

    }

    public static string? GetIniKey(string line)
    {
        if (IsComment(line)) return null;

        var split = line.Split('=');
        return split.Length != 2 ? split.FirstOrDefault()?.Trim() : split[0].Trim();
    }

    public static bool IsComment(string line) => line.Trim().StartsWith(";");

    public static bool IsSection(string line, string? sectionKey = null)
    {
        line = line.Trim();
        if (!line.StartsWith("[") && !line.EndsWith("]"))
            return false;


        return sectionKey is null || line.Equals($"[{sectionKey}]", StringComparison.CurrentCultureIgnoreCase) ||
               line.Equals($"{sectionKey}", StringComparison.CurrentCultureIgnoreCase);
    }

    public static bool IsIniKey(string line, string key) =>
        line.Trim().StartsWith(key, StringComparison.CurrentCultureIgnoreCase);

    public static string? FormatIniKey(string key, string? value) =>
        value is not null ? $"{key} = {value}" : null;
}