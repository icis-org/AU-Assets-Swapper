using System;
using System.IO;
using UnityEngine;

namespace AU_Assets_Swapper;

internal static class AudioLoader
{
    public static AudioClip LoadAudioClip(string filePath, string clipName)
    {
        if (!File.Exists(filePath))
            return null;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var data = File.ReadAllBytes(filePath);

        switch (ext)
        {
            case ".wav":
                return LoadWav(data, clipName);
            case ".ogg":
                return LoadOgg(data, clipName);
            default:
                Plugin.LogSource.LogWarning($"[AUAS] Unsupported audio format: {ext}. Use WAV or OGG.");
                return null;
        }
    }

    private static AudioClip LoadWav(byte[] data, string clipName)
    {
        try
        {
            int channels = BitConverter.ToInt16(data, 22);
            int sampleRate = BitConverter.ToInt32(data, 24);
            int bitsPerSample = BitConverter.ToInt16(data, 34);
            int bytesPerSample = bitsPerSample / 8;

            int headerOffset = 44;
            while (headerOffset < data.Length - 8)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(data, headerOffset, 4);
                int chunkSize = BitConverter.ToInt32(data, headerOffset + 4);
                if (chunkId == "data")
                    break;
                headerOffset += 8 + chunkSize;
            }

            if (headerOffset >= data.Length)
                return null;

            int dataSize = BitConverter.ToInt32(data, headerOffset + 4);
            int sampleCount = dataSize / (channels * bytesPerSample);
            headerOffset += 8;

            var samples = new float[sampleCount * channels];
            for (int i = 0; i < samples.Length; i++)
            {
                int byteIndex = headerOffset + i * bytesPerSample;
                if (byteIndex + bytesPerSample > data.Length)
                    break;

                if (bitsPerSample == 16)
                    samples[i] = BitConverter.ToInt16(data, byteIndex) / 32768f;
                else if (bitsPerSample == 8)
                    samples[i] = (data[byteIndex] - 128) / 128f;
            }

            var clip = AudioClip.Create(clipName, sampleCount, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] WAV parse error: {ex.Message}");
            return null;
        }
    }

    private static AudioClip LoadOgg(byte[] data, string clipName)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "auas_" + Guid.NewGuid().ToString("N") + ".ogg");
            File.WriteAllBytes(tempPath, data);

            var clip = Resources.Load<AudioClip>(tempPath);
            try { File.Delete(tempPath); } catch { }

            if (clip != null)
                clip.name = clipName;
            return clip;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] OGG load error: {ex.Message}");
            return null;
        }
    }
}
