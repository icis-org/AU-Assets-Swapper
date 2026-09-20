using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AU_Assets_Swapper.Utils;

internal static class AssetLogger
{
    private static readonly List<string> _loggedAssets = new();

    public static void LogAsset(string assetPath, Type assetType)
    {
        if (!Plugin.DumpAllAssets.Value)
            return;

        var logEntry = $"[{assetType.Name}] {assetPath}";
        if (!_loggedAssets.Contains(logEntry))
        {
            _loggedAssets.Add(logEntry);
            Plugin.LogSource.LogInfo($"[AUAS-DUMP] {logEntry}");
        }
    }

    public static void SaveDumpToFile()
    {
        if (_loggedAssets.Count == 0)
            return;

        try
        {
            var dumpPath = Path.Combine(Plugin.SwapRootPath, "AssetDump.txt");
            // old: File.WriteAllLines(dumpPath, _loggedAssets);
            File.WriteAllLines(dumpPath, _loggedAssets);
            Plugin.LogSource.LogInfo($"[AUAS] Asset dump saved to: {dumpPath} ({_loggedAssets.Count} entries)");
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Failed to save asset dump: {ex.Message}");
        }
    }

    public static void Clear() => _loggedAssets.Clear();
    public static int Count => _loggedAssets.Count;
}
