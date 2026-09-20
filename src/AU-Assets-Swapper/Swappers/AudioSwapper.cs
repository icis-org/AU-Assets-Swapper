using UnityEngine;

namespace AU_Assets_Swapper.Swappers;

internal static class AudioSwapper
{
    public static AudioClip ReplaceClipData(AudioClip original, AudioClip replacement)
    {
        if (original == null || replacement == null) return original;
        return replacement;
    }
}
