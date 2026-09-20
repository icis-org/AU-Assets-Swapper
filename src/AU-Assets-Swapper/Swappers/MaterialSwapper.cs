using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class MaterialSwapper
{
    public static Material ReplaceMaterialData(Material original, Material replacement)
    {
        if (original == null || replacement == null) return original;

        var clonedMaterial = new Material(replacement);
        clonedMaterial.name = original.name;
        if (original.renderQueue != replacement.renderQueue)
            clonedMaterial.renderQueue = original.renderQueue;
        return clonedMaterial;
    }
}
