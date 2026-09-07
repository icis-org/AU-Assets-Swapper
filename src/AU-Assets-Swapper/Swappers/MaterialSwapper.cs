using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class MaterialSwapper
{
    public static Material ReplaceMaterialData(Material original, Material replacement)
    {
        if (original == null || replacement == null) return original;

        var newMat = new Material(replacement);
        newMat.name = original.name;
        if (original.renderQueue != replacement.renderQueue)
            newMat.renderQueue = original.renderQueue;
        return newMat;
    }
}
