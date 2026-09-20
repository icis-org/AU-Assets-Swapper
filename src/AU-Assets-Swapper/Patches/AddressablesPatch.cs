using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace AU_Assets_Swapper.Patches;

internal static class AddressablesPatch
{
    private static int _addressablesPatchLogged;

    public static void Patch(Harmony harmony)
    {
        try
        {
            var addressablesType = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
            if (addressablesType == null)
            {
                Plugin.LogSource.LogWarning("[AUAS] Addressables type not found, skipping Addressables patch.");
                return;
            }

            Plugin.LogSource.LogInfo("[AUAS] Found Addressables type, attempting to patch LoadAsset methods...");

            var methods = addressablesType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var patchedCount = 0;

            foreach (var method in methods)
            {
                if (method.IsGenericMethod) continue;
                if (method.ContainsGenericParameters) continue;

                try
                {
                    var parameters = method.GetParameters();
                    if (method.Name == "LoadAsset" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        var prefix = new HarmonyMethod(AccessTools.Method(typeof(AddressablesPatch), nameof(LoadAssetPrefix)));
                        harmony.Patch(method, prefix);
                        patchedCount++;
                    }
                }
                catch
                {
                    // Harmony already logs patch failures
                }
            }

            Plugin.LogSource.LogInfo($"[AUAS] Addressables: patched {patchedCount} methods.");
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Addressables patch failed: {ex.Message}");
        }
    }

    internal static bool LoadAssetPrefix(string key, ref object __result)
    {
        if (Interlocked.CompareExchange(ref _addressablesPatchLogged, 1, 0) == 0)
            Plugin.LogSource.LogInfo("[AUAS] Addressables.LoadAsset(string) prefix CALLED");

        if (string.IsNullOrEmpty(key))
            return true;

        var manager = Plugin.SwapManager;
        if (manager == null)
            return true;

        manager.LogLoadedAsset($"[Addressable] {key}", typeof(object));
        return true;
    }
}
