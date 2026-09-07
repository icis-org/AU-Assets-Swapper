using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class PrefabSwapper
{
    public static GameObject InstantiateReplacement(GameObject original, GameObject replacement)
    {
        if (original == null || replacement == null) return original;

        var instance = Object.Instantiate(replacement);
        if (instance == null) return original;

        instance.name = original.name;
        instance.transform.position = original.transform.position;
        instance.transform.rotation = original.transform.rotation;
        instance.transform.localScale = original.transform.localScale;
        return instance;
    }
}
