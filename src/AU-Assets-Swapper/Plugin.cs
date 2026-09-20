using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using AU_Assets_Swapper.Patches;

namespace AU_Assets_Swapper;

[BepInPlugin("com.auassetsswapper.plugin", "AU Assets Swapper", "1.0.0")]
[BepInProcess("Among Us.exe")]
public class Plugin : BasePlugin
{
    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource LogSource { get; private set; }
    internal static string PluginPath { get; private set; }
    internal static string SwapRootPath { get; private set; }
    internal static Harmony HarmonyInstance { get; private set; }
    internal static AssetSwapManager SwapManager { get; private set; }

    internal static ConfigEntry<bool> DumpAllAssets { get; private set; }
    internal static ConfigEntry<bool> EnableSpriteSwap { get; private set; }
    internal static ConfigEntry<bool> EnableTextureSwap { get; private set; }
    internal static ConfigEntry<bool> EnableAudioSwap { get; private set; }
    internal static ConfigEntry<bool> EnableFontSwap { get; private set; }
    internal static ConfigEntry<bool> EnableShaderSwap { get; private set; }
    internal static ConfigEntry<bool> EnableMaterialSwap { get; private set; }
    internal static ConfigEntry<bool> EnablePrefabSwap { get; private set; }

    public override void Load()
    {
        Instance = this;
        LogSource = Log;

        PluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        SwapRootPath = Path.Combine(
            Directory.GetParent(PluginPath).Parent.FullName,
            "AUAS_Data"
        );

        InitConfig();
        CreateSwapDirectories();

        LogSource.LogInfo($"[AUAS] Plugin path: {PluginPath}");
        LogSource.LogInfo($"[AUAS] Swap root: {SwapRootPath}");

        SwapManager = new AssetSwapManager(SwapRootPath);
        SwapManager.ScanAndLoadAssets();

        HarmonyInstance = new Harmony("com.auassetsswapper.plugin");

        try { ResourcesLoadPatch.Patch(HarmonyInstance); }
        catch (Exception ex) { LogSource.LogWarning($"[AUAS] Resources patch error: {ex.Message}"); }

        try { AssetBundlePatch.Patch(HarmonyInstance); }
        catch (Exception ex) { LogSource.LogWarning($"[AUAS] AssetBundle patch error: {ex.Message}"); }

        try { AddressablesPatch.Patch(HarmonyInstance); }
        catch (Exception ex) { LogSource.LogWarning($"[AUAS] Addressables patch error: {ex.Message}"); }

        AddComponent<SwapManagerComponent>();

        LogSource.LogInfo("[AUAS] Plugin loaded. Runtime scanner active.");
    }

    public override bool Unload()
    {
        HarmonyInstance?.UnpatchSelf();
        LogSource.LogInfo("[AUAS] Plugin unloaded, Harmony patches removed.");
        return true;
    }

    private void InitConfig()
    {
        DumpAllAssets = Config.Bind(
            "General", "DumpAllAssets", true,
            "Log all loaded asset names to the BepInEx console (useful for discovering replaceable assets)"
        );

        // Keep the defaults simple and forgiving; users can turn individual swap types off when needed.
        EnableSpriteSwap = Config.Bind("Swappers", "Sprites", true, "Enable sprite/texture swapping");
        EnableTextureSwap = Config.Bind("Swappers", "Textures", true, "Enable raw Texture2D swapping");
        EnableAudioSwap = Config.Bind("Swappers", "Audio", true, "Enable audio clip swapping");
        EnableFontSwap = Config.Bind("Swappers", "Fonts", true, "Enable font swapping");
        EnableShaderSwap = Config.Bind("Swappers", "Shaders", true, "Enable shader swapping");
        EnableMaterialSwap = Config.Bind("Swappers", "Materials", true, "Enable material swapping");
        EnablePrefabSwap = Config.Bind("Swappers", "Prefabs", true, "Enable prefab/gameobject swapping");
    }

    private void CreateSwapDirectories()
    {
        string[] categories = { "Sprites", "Textures", "Audio", "Fonts", "Shaders", "Materials", "Prefabs" };
        foreach (var category in categories)
        {
            var directory = Path.Combine(SwapRootPath, category);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        var readmePath = Path.Combine(SwapRootPath, "README.txt");
        if (!File.Exists(readmePath))
        {
            // old: File.WriteAllText(readmePath, "...long string...");
            File.WriteAllText(readmePath,
                "AU-Assets-Swapper swap folder\n" +
                "=============================\n\n" +
                "Place replacement assets in the appropriate subfolder:\n\n" +
                "  Sprites/   - PNG/JPG images replace Sprite or Texture2D assets\n" +
                "  Textures/  - PNG/JPG images replace raw Texture2D assets\n" +
                "  Audio/     - WAV/OGG files replace AudioClip assets\n" +
                "  Fonts/     - TTF/OTF files replace Font assets\n" +
                "  Shaders/   - AssetBundle files (.ab) replace Shader assets\n" +
                "  Materials/ - AssetBundle files (.ab) replace Material assets\n" +
                "  Prefabs/   - AssetBundle files (.ab) replace Prefab/GameObject assets\n\n" +
                "File names should match the asset name (without extension).\n" +
                "Enable DumpAllAssets in the config to see all loaded asset names.\n" +
                "Press F5 in-game to hot-reload replacements.\n"
            );
        }
    }
}
