using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class SpriteSwapper
{
    public static Sprite CreateFromTexture(Texture2D texture, string spriteName, float pixelsPerUnit = 100f)
    {
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    // TODO: actually apply the 9-slice border
    public static Sprite CreateSliceSprite(Texture2D texture, string spriteName, Vector4 border, float pixelsPerUnit = 100f)
    {
        if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }
}
