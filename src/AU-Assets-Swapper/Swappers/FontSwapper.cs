using System.IO;
using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class FontSwapper
{
    public static Font LoadFromTtf(string filePath, string fontName)
    {
        if (!File.Exists(filePath)) return null;
        try
        {
            var font = new Font(filePath);
            font.name = fontName;
            return font;
        }
        catch (System.Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Failed to load TTF '{fontName}': {ex.Message}");
            return null;
        }
    }
}
