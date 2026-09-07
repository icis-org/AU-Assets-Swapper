using System;
using System.Collections.Generic;
using UnityEngine;

namespace AU_Assets_Swapper;

public class SwapManagerComponent : MonoBehaviour
{
    private float _nextScanTime;
    private const float ScanInterval = 2f;
    private readonly HashSet<int> _processedObjects = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Plugin.SwapManager?.Rescan();
            _processedObjects.Clear();
            Plugin.LogSource.LogInfo("[AUAS] Assets rescanned (F5 pressed).");
        }

        if (Time.time < _nextScanTime) return;
        _nextScanTime = Time.time + ScanInterval;

        ScanAndReplace();
    }

    private void ScanAndReplace()
    {
        var manager = Plugin.SwapManager;
        if (manager == null) return;

        try
        {
            ReplaceSpriteRenderers(manager);
            ReplaceRenderers(manager);
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Scan error: {ex.Message}");
        }
    }

    private void ReplaceSpriteRenderers(AssetSwapManager manager)
    {
        var renderers = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;
            if (!sr.gameObject.activeInHierarchy) continue;

            var id = sr.GetInstanceID();
            if (_processedObjects.Contains(id)) continue;

            var spriteName = sr.sprite.name;
            if (manager.HasSpriteReplacement(spriteName))
            {
                var replacement = manager.LoadReplacementSprite(spriteName);
                if (replacement != null)
                {
                    sr.sprite = replacement;
                    _processedObjects.Add(id);
                }
            }
            else if (manager.HasTextureReplacement(spriteName))
            {
                var tex = manager.LoadReplacementTexture(spriteName);
                if (tex != null)
                {
                    var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), sr.sprite.pixelsPerUnit);
                    newSprite.name = spriteName;
                    sr.sprite = newSprite;
                    _processedObjects.Add(id);
                }
            }
        }
    }

    private void ReplaceRenderers(AssetSwapManager manager)
    {
        var renderers = FindObjectsOfType<Renderer>();
        foreach (var r in renderers)
        {
            if (r == null || !r.gameObject.activeInHierarchy) continue;
            if (r is SpriteRenderer) continue;

            var id = r.GetInstanceID();
            if (_processedObjects.Contains(id)) continue;

            var mat = r.material;
            if (mat == null) continue;
            if (!mat.HasProperty("_MainTex")) continue;
            if (mat.mainTexture == null) continue;

            var texName = mat.mainTexture.name;
            if (manager.HasTextureReplacement(texName))
            {
                var replacement = manager.LoadReplacementTexture(texName);
                if (replacement != null)
                {
                    mat.mainTexture = replacement;
                    _processedObjects.Add(id);
                }
            }
        }
    }
}
