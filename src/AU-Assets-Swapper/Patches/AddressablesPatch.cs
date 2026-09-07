using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace AU_Assets_Swapper.Patches;

internal static class AddressablesPatch
{
    private static int _patchAttempted;

    public static void Patch(Harmony harmony)
    {
        try
        {
            var addrType = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
            if (addrType == null)
            {
                Plugin.LogSource.LogWarning("[AUAS] Addressables type not found, skipping Addressables patch.");
                return;
            }

            Plugin.LogSource.LogInfo("[AUAS] Found Addressables type, attempting to patch LoadAsset methods...");

            var methods = addrType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            int patched = 0;

            foreach (var method in methods)
            {
                if (method.IsGenericMethod) continue;
                if (method.ContainsGenericParameters) continue;

                try
                {
                    var parms = method.GetParameters();
                    if (method.Name == "LoadAsset" && parms.Length == 1 && parms[0].ParameterType == typeof(string))
                    {
                        var prefix = new HarmonyMethod(AccessTools.Method(typeof(AddressablesPatch), nameof(LoadAssetPrefix)));
                        harmony.Patch(method, prefix);
                        patched++;
                    }
                }
                catch { }
            }

            Plugin.LogSource.LogInfo($"[AUAS] Addressables: patched {patched} methods.");
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Addressables patch failed: {ex.Message}");
        }
    }

    internal static bool LoadAssetPrefix(string key, ref object __result)
    {
        if (Interlocked.CompareExchange(ref _patchAttempted, 1, 0) == 0)
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
