using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace AU_Assets_Swapper.Patches;

internal static class ResourcesLoadPatch
{
    private static int _stringLoadLogged;
    private static int _typedLoadLogged;
    private static int _loadAllLogged;
    private static readonly HarmonyMethod _loadPrefix = new(AccessTools.Method(typeof(ResourcesLoadPatch), nameof(LoadPrefix)));
    private static readonly HarmonyMethod _loadTypedPrefix = new(AccessTools.Method(typeof(ResourcesLoadPatch), nameof(LoadTypedPrefix)));
    private static readonly HarmonyMethod _loadAllPrefix = new(AccessTools.Method(typeof(ResourcesLoadPatch), nameof(LoadAllPrefix)));

    public static void Patch(Harmony harmony)
    {
        var methods = typeof(Resources).GetMethods(BindingFlags.Public | BindingFlags.Static);
        foreach (var method in methods)
        {
            if (method.IsGenericMethod) continue;
            if (method.ContainsGenericParameters) continue;

            try
            {
                var parameters = method.GetParameters();
                if (method.Name == "Load" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    harmony.Patch(method, _loadPrefix);
                else if (method.Name == "Load" && parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(Type))
                    harmony.Patch(method, _loadTypedPrefix);
                else if (method.Name == "LoadAll" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    harmony.Patch(method, _loadAllPrefix);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[AUAS] Failed to patch Resources.{method.Name}: {ex.Message}");
            }
        }
    }

    internal static bool LoadPrefix(string path, ref UnityEngine.Object __result)
    {
        if (Interlocked.CompareExchange(ref _stringLoadLogged, 1, 0) == 0)
            Plugin.LogSource.LogInfo("[AUAS] Resources.Load(string) prefix CALLED");

        if (string.IsNullOrEmpty(path))
            return true;

        var manager = Plugin.SwapManager;
        if (manager == null)
            return true;

        var assetName = ExtractAssetName(path);
        manager.LogLoadedAsset(path, typeof(UnityEngine.Object));

        if (manager.HasTextureReplacement(assetName))
        {
            var texture = manager.LoadReplacementTexture(assetName);
            if (texture != null) { __result = texture; return false; }
        }

        if (manager.HasSpriteReplacement(assetName))
        {
            var sprite = manager.LoadReplacementSprite(assetName);
            if (sprite != null) { __result = sprite; return false; }
        }

        if (manager.HasAudioReplacement(assetName))
        {
            var clip = manager.LoadReplacementAudio(assetName);
            if (clip != null) { __result = clip; return false; }
        }

        if (manager.HasFontReplacement(assetName))
        {
            var font = manager.LoadReplacementFont(assetName);
            if (font != null) { __result = font; return false; }
        }

        if (manager.HasShaderReplacement(assetName))
        {
            var shader = manager.LoadReplacementShader(assetName);
            if (shader != null) { __result = shader; return false; }
        }

        if (manager.HasMaterialReplacement(assetName))
        {
            var material = manager.LoadReplacementMaterial(assetName);
            if (material != null) { __result = material; return false; }
        }

        if (manager.HasPrefabReplacement(assetName))
        {
            var prefab = manager.LoadReplacementPrefab(assetName);
            if (prefab != null) { __result = prefab; return false; }
        }

        return true;
    }

    internal static bool LoadTypedPrefix(string path, Type systemTypeInstance, ref UnityEngine.Object __result)
    {
        if (Interlocked.CompareExchange(ref _typedLoadLogged, 1, 0) == 0)
            Plugin.LogSource.LogInfo("[AUAS] Resources.Load(string, Type) prefix CALLED");

        if (string.IsNullOrEmpty(path))
            return true;

        var manager = Plugin.SwapManager;
        if (manager == null)
            return true;

        var assetName = ExtractAssetName(path);
        manager.LogLoadedAsset(path, systemTypeInstance ?? typeof(UnityEngine.Object));

        if (systemTypeInstance == typeof(Texture2D) && manager.HasTextureReplacement(assetName))
        {
            var texture = manager.LoadReplacementTexture(assetName);
            if (texture != null) { __result = texture; return false; }
        }

        if (systemTypeInstance == typeof(Sprite) && manager.HasSpriteReplacement(assetName))
        {
            var sprite = manager.LoadReplacementSprite(assetName);
            if (sprite != null) { __result = sprite; return false; }
        }

        if (systemTypeInstance == typeof(AudioClip) && manager.HasAudioReplacement(assetName))
        {
            var clip = manager.LoadReplacementAudio(assetName);
            if (clip != null) { __result = clip; return false; }
        }

        if (systemTypeInstance == typeof(Font) && manager.HasFontReplacement(assetName))
        {
            var font = manager.LoadReplacementFont(assetName);
            if (font != null) { __result = font; return false; }
        }

        if (systemTypeInstance == typeof(Shader) && manager.HasShaderReplacement(assetName))
        {
            var shader = manager.LoadReplacementShader(assetName);
            if (shader != null) { __result = shader; return false; }
        }

        if (systemTypeInstance == typeof(Material) && manager.HasMaterialReplacement(assetName))
        {
            var material = manager.LoadReplacementMaterial(assetName);
            if (material != null) { __result = material; return false; }
        }

        if (systemTypeInstance == typeof(GameObject) && manager.HasPrefabReplacement(assetName))
        {
            var prefab = manager.LoadReplacementPrefab(assetName);
            if (prefab != null) { __result = prefab; return false; }
        }

        return true;
    }

    internal static bool LoadAllPrefix(string path, ref UnityEngine.Object[] __result)
    {
        if (Interlocked.CompareExchange(ref _loadAllLogged, 1, 0) == 0)
            Plugin.LogSource.LogInfo("[AUAS] Resources.LoadAll(string) prefix CALLED");

        if (string.IsNullOrEmpty(path))
            return true;

        var manager = Plugin.SwapManager;
        if (manager == null)
            return true;

        manager.LogLoadedAsset(path + " (LoadAll)", typeof(UnityEngine.Object));
        return true;
    }

    private static string ExtractAssetName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var lastSlash = path.LastIndexOf('/');
        if (lastSlash >= 0)
            return path.Substring(lastSlash + 1);

        var lastBackslash = path.LastIndexOf('\\');
        if (lastBackslash >= 0)
            return path.Substring(lastBackslash + 1);

        return path;
    }
}
