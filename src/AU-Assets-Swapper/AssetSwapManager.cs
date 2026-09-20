using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AU_Assets_Swapper;

internal class AssetSwapManager
{
    private readonly string _rootPath;

    // case-insensitive so Unity asset names match regardless of OS casing
    private readonly Dictionary<string, string> _spriteReplacements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _textureReplacements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _audioReplacements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _fontReplacements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _shaderReplacements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _materialReplacements = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _prefabReplacements = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AudioClip> _audioCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Font> _fontCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, AssetBundle> _loadedBundles = new(StringComparer.OrdinalIgnoreCase);

    public AssetSwapManager(string rootPath)
    {
        _rootPath = rootPath;
    }

    public void ScanAndLoadAssets()
    {
        ClearReplacementMaps();

        ScanCategory("Sprites", _spriteReplacements);
        ScanCategory("Textures", _textureReplacements);
        ScanCategory("Audio", _audioReplacements);
        ScanCategory("Fonts", _fontReplacements);
        ScanCategory("Shaders", _shaderReplacements);
        ScanCategory("Materials", _materialReplacements);
        ScanCategory("Prefabs", _prefabReplacements);

        var totalAssetCount = _spriteReplacements.Count + _textureReplacements.Count + _audioReplacements.Count +
                              _fontReplacements.Count + _shaderReplacements.Count + _materialReplacements.Count +
                              _prefabReplacements.Count;

        Plugin.LogSource.LogInfo($"[AUAS] Scanned: {_spriteReplacements.Count} sprites, {_textureReplacements.Count} textures, " +
                           $"{_audioReplacements.Count} audio, {_fontReplacements.Count} fonts, " +
                           $"{_shaderReplacements.Count} shaders, {_materialReplacements.Count} materials, " +
                           $"{_prefabReplacements.Count} prefabs - total {totalAssetCount} replacements.");
    }

    private void ClearReplacementMaps()
    {
        _spriteReplacements.Clear();
        _textureReplacements.Clear();
        _audioReplacements.Clear();
        _fontReplacements.Clear();
        _shaderReplacements.Clear();
        _materialReplacements.Clear();
        _prefabReplacements.Clear();
    }

    public void Rescan()
    {
        foreach (var bundle in _loadedBundles.Values)
            bundle?.Unload(false);
        _loadedBundles.Clear();

        _spriteCache.Clear();
        _textureCache.Clear();
        _audioCache.Clear();
        _fontCache.Clear();

        ScanAndLoadAssets();
    }

    private void ScanCategory(string category, Dictionary<string, string> replacementMap)
    {
        var categoryPath = Path.Combine(_rootPath, category);
        if (!Directory.Exists(categoryPath))
            return;

        var assetFiles = Directory.GetFiles(categoryPath, "*.*", SearchOption.AllDirectories);
        foreach (var filePath in assetFiles)
        {
            if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                continue;

            var assetName = Path.GetFileNameWithoutExtension(filePath);
            replacementMap[assetName] = filePath;
        }
    }

    public bool HasSpriteReplacement(string name) => Plugin.EnableSpriteSwap.Value && _spriteReplacements.ContainsKey(name);
    public bool HasTextureReplacement(string name) => Plugin.EnableTextureSwap.Value && _textureReplacements.ContainsKey(name);
    public bool HasAudioReplacement(string name) => Plugin.EnableAudioSwap.Value && _audioReplacements.ContainsKey(name);
    public bool HasFontReplacement(string name) => Plugin.EnableFontSwap.Value && _fontReplacements.ContainsKey(name);
    public bool HasShaderReplacement(string name) => Plugin.EnableShaderSwap.Value && _shaderReplacements.ContainsKey(name);
    public bool HasMaterialReplacement(string name) => Plugin.EnableMaterialSwap.Value && _materialReplacements.ContainsKey(name);
    public bool HasPrefabReplacement(string name) => Plugin.EnablePrefabSwap.Value && _prefabReplacements.ContainsKey(name);

    public bool HasAnyReplacement()
    {
        return _spriteReplacements.Count > 0 || _textureReplacements.Count > 0 ||
               _audioReplacements.Count > 0 || _fontReplacements.Count > 0 ||
               _shaderReplacements.Count > 0 || _materialReplacements.Count > 0 ||
               _prefabReplacements.Count > 0;
    }

    public Sprite LoadReplacementSprite(string assetName)
    {
        if (_spriteCache.TryGetValue(assetName, out var cachedSprite))
            return cachedSprite;

        if (!_spriteReplacements.TryGetValue(assetName, out var filePath))
            return null;

        try
        {
            var texture = ImageLoader.LoadTexture2D(filePath);
            if (texture == null)
            {
                Plugin.LogSource.LogWarning($"[AUAS] Failed to load texture for sprite: {assetName}");
                return null;
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = assetName;

            _spriteCache[assetName] = sprite;
            Plugin.LogSource.LogInfo($"[AUAS] Loaded sprite replacement: {assetName} ({texture.width}x{texture.height})");
            return sprite;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Error loading sprite '{assetName}': {ex.Message}");
            return null;
        }
    }

    public Texture2D LoadReplacementTexture(string assetName)
    {
        if (_textureCache.TryGetValue(assetName, out var cachedTexture))
            return cachedTexture;

        if (!_textureReplacements.TryGetValue(assetName, out var filePath))
            return null;

        try
        {
            var texture = ImageLoader.LoadTexture2D(filePath);
            if (texture == null)
            {
                Plugin.LogSource.LogWarning($"[AUAS] Failed to load texture: {assetName}");
                return null;
            }

            texture.name = assetName;
            _textureCache[assetName] = texture;
            Plugin.LogSource.LogInfo($"[AUAS] Loaded texture replacement: {assetName} ({texture.width}x{texture.height})");
            return texture;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Error loading texture '{assetName}': {ex.Message}");
            return null;
        }
    }

    public AudioClip LoadReplacementAudio(string assetName)
    {
        if (_audioCache.TryGetValue(assetName, out var cachedClip))
            return cachedClip;

        if (!_audioReplacements.TryGetValue(assetName, out var filePath))
            return null;

        try
        {
            var clip = AudioLoader.LoadAudioClip(filePath, assetName);
            if (clip == null)
            {
                Plugin.LogSource.LogWarning($"[AUAS] Failed to load audio: {assetName}");
                return null;
            }

            _audioCache[assetName] = clip;
            Plugin.LogSource.LogInfo($"[AUAS] Loaded audio replacement: {assetName}");
            return clip;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Error loading audio '{assetName}': {ex.Message}");
            return null;
        }
    }

    public Font LoadReplacementFont(string assetName)
    {
        if (_fontCache.TryGetValue(assetName, out var cachedFont))
            return cachedFont;

        if (!_fontReplacements.TryGetValue(assetName, out var filePath))
            return null;

        try
        {
            var font = new Font(filePath);
            font.name = assetName;

            _fontCache[assetName] = font;
            Plugin.LogSource.LogInfo($"[AUAS] Loaded font replacement: {assetName}");
            return font;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Error loading font '{assetName}': {ex.Message}");
            return null;
        }
    }

    public Shader LoadReplacementShader(string assetName)
    {
        if (!_shaderReplacements.TryGetValue(assetName, out var filePath))
            return null;
        return LoadFromBundle<Shader>(assetName, filePath);
    }

    public Material LoadReplacementMaterial(string assetName)
    {
        if (!_materialReplacements.TryGetValue(assetName, out var filePath))
            return null;
        return LoadFromBundle<Material>(assetName, filePath);
    }

    public GameObject LoadReplacementPrefab(string assetName)
    {
        if (!_prefabReplacements.TryGetValue(assetName, out var filePath))
            return null;
        return LoadFromBundle<GameObject>(assetName, filePath);
    }

    private T LoadFromBundle<T>(string assetName, string bundlePath) where T : UnityEngine.Object
    {
        try
        {
            if (!_loadedBundles.TryGetValue(bundlePath, out var bundle) || bundle == null)
            {
                bundle = AssetBundle.LoadFromFile(bundlePath);
                if (bundle == null)
                {
                    Plugin.LogSource.LogWarning($"[AUAS] Failed to load asset bundle: {bundlePath}");
                    return null;
                }
                _loadedBundles[bundlePath] = bundle;
            }

            var bundleAsset = bundle.LoadAsset(assetName);
            var loadedAsset = bundleAsset as T;
            if (loadedAsset != null)
                Plugin.LogSource.LogInfo($"[AUAS] Loaded {typeof(T).Name} replacement from bundle: {assetName}");
            return loadedAsset;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError($"[AUAS] Error loading {typeof(T).Name} '{assetName}' from bundle: {ex.Message}");
            return null;
        }
    }

    public void LogLoadedAsset(string assetName, Type assetType)
    {
        if (!Plugin.DumpAllAssets.Value)
            return;

        Plugin.LogSource.LogInfo($"[AUAS-DUMP] {assetType.Name}: {assetName}");
    }
}
