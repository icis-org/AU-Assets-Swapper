using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace AU_Assets_Swapper.Patches;

internal static class AssetBundlePatch
{
    private static int _loadAssetFired;
    private static int _loadAssetAsyncFired;
    private static readonly HarmonyMethod _loadAssetPrefix = new(AccessTools.Method(typeof(AssetBundlePatch), nameof(LoadAssetPrefix)));
    private static readonly HarmonyMethod _loadAssetAsyncPrefix = new(AccessTools.Method(typeof(AssetBundlePatch), nameof(LoadAssetAsyncPrefix)));

    public static void Patch(Harmony harmony)
    {
        var methods = typeof(AssetBundle).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (var method in methods)
        {
            if (method.IsGenericMethod) continue;
            if (method.ContainsGenericParameters) continue;

            try
            {
                var parms = method.GetParameters();
                if (method.Name == "LoadAsset" && parms.Length == 1 && parms[0].ParameterType == typeof(string))
                    harmony.Patch(method, _loadAssetPrefix);
                else if (method.Name == "LoadAssetAsync" && parms.Length == 1 && parms[0].ParameterType == typeof(string))
                    harmony.Patch(method, _loadAssetAsyncPrefix);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogWarning($"[AUAS] Failed to patch AssetBundle.{method.Name}: {ex.Message}");
            }
        }
    }

    internal static bool LoadAssetPrefix(AssetBundle __instance, string name, ref UnityEngine.Object __result)
    {
        if (Interlocked.CompareExchange(ref _loadAssetFired, 1, 0) == 0)
            Plugin.LogSource.LogInfo("[AUAS] AssetBundle.LoadAsset(string) prefix CALLED");

        if (string.IsNullOrEmpty(name))
            return true;

        var manager = Plugin.SwapManager;
        if (manager == null)
            return true;

        manager.LogLoadedAsset($"[Bundle] {name}", typeof(UnityEngine.Object));

        if (manager.HasTextureReplacement(name))
        {
            var tex = manager.LoadReplacementTexture(name);
            if (tex != null) { __result = tex; return false; }
        }

        if (manager.HasSpriteReplacement(name))
        {
            var sprite = manager.LoadReplacementSprite(name);
            if (sprite != null) { __result = sprite; return false; }
        }

        if (manager.HasAudioReplacement(name))
        {
            var clip = manager.LoadReplacementAudio(name);
            if (clip != null) { __result = clip; return false; }
        }

        if (manager.HasFontReplacement(name))
        {
            var font = manager.LoadReplacementFont(name);
            if (font != null) { __result = font; return false; }
        }

        if (manager.HasShaderReplacement(name))
        {
            var shader = manager.LoadReplacementShader(name);
            if (shader != null) { __result = shader; return false; }
        }

        if (manager.HasMaterialReplacement(name))
        {
            var mat = manager.LoadReplacementMaterial(name);
            if (mat != null) { __result = mat; return false; }
        }

        if (manager.HasPrefabReplacement(name))
        {
            var prefab = manager.LoadReplacementPrefab(name);
            if (prefab != null) { __result = prefab; return false; }
        }

        return true;
    }

    internal static bool LoadAssetAsyncPrefix(AssetBundle __instance, string name, ref AssetBundleRequest __result)
    {
        if (Interlocked.CompareExchange(ref _loadAssetAsyncFired, 1, 0) == 0)
            Plugin.LogSource.LogInfo("[AUAS] AssetBundle.LoadAssetAsync(string) prefix CALLED");

        if (string.IsNullOrEmpty(name))
            return true;

        var manager = Plugin.SwapManager;
        if (manager == null)
            return true;

        manager.LogLoadedAsset($"[Bundle/Async] {name}", typeof(UnityEngine.Object));
        return true;
    }
}
