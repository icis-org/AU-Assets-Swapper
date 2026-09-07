using System;
using System.IO;
using UnityEngine;

namespace AU_Assets_Swapper;

internal static class ImageLoader
{
    public static Texture2D LoadTexture2D(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var bytes = File.ReadAllBytes(filePath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        try
        {
            bool loaded = tex.LoadImage(bytes);
            if (loaded)
                return tex;

            UnityEngine.Object.Destroy(tex);
            return null;
        }
        catch
        {
            UnityEngine.Object.Destroy(tex);
            return null;
        }
    }
}
