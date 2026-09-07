using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class SpriteSwapper
{
    public static Sprite CreateFromTexture(Texture2D tex, string spriteName, float pixelsPerUnit = 100f)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    public static Sprite CreateSliceSprite(Texture2D tex, string spriteName, Vector4 border, float pixelsPerUnit = 100f)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
