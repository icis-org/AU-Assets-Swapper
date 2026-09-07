using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class ShaderSwapper
{
    public static Shader FindShaderInBundle(AssetBundle bundle, string shaderName)
    {
        if (bundle == null) return null;

        var assets = bundle.LoadAllAssets();
        foreach (var asset in assets)
        {
            var shader = asset as Shader;
            if (shader == null) continue;

            if (shader.name == shaderName || shader.name.EndsWith("/" + shaderName))
                return shader;
        }

        foreach (var asset in assets)
        {
            var shader = asset as Shader;
            if (shader != null)
            {
                Plugin.LogSource.LogWarning($"[AUAS] Shader '{shaderName}' not found, using: {shader.name}");
                return shader;
            }
        }

        return null;
    }
}
