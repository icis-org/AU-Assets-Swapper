using System;
using UnityEngine;

namespace AU_Assets_Swapper;

public class SwapManagerComponent : MonoBehaviour
{
    private float _nextScanTime;
    private const float ScanInterval = 0.2f;
    private const float RetryDuration = 3f;
    private float _retryUntil;
    private string _lastScene = "";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Plugin.SwapManager?.Rescan();
            Plugin.LogSource.LogInfo("[AUAS] Assets rescanned (F5 pressed).");
        }

        float now = Time.time;
        bool aggressiveScan = now < _retryUntil;

        if (!aggressiveScan && now < _nextScanTime) return;
        _nextScanTime = now + ScanInterval;

        ScanAndReplace();
    }

    private void ScanAndReplace()
    {
        var manager = Plugin.SwapManager;
        if (manager == null) return;

        try
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene != _lastScene)
            {
                _lastScene = scene;
                _retryUntil = Time.time + RetryDuration;
                manager.Rescan();
                Plugin.LogSource.LogInfo($"[AUAS] Scene changed to '{scene}', rescanning for {RetryDuration}s");
            }

            int spriteCount = ReplaceSpriteRenderers(manager);
            int rendererCount = ReplaceRenderers(manager);

            if (spriteCount == 0 && rendererCount == 0 && manager.HasAnyReplacement())
            {
                _retryUntil = Time.time + RetryDuration;
            }
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Scan error: {ex.Message}");
        }
    }

    private int ReplaceSpriteRenderers(AssetSwapManager manager)
    {
        var renderers = FindObjectsOfType<SpriteRenderer>();
        int replaced = 0;

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null) continue;
            if (!sr.gameObject.activeInHierarchy) continue;

            var spriteName = sr.sprite.name;
            if (manager.HasSpriteReplacement(spriteName))
            {
                var replacement = manager.LoadReplacementSprite(spriteName);
                if (replacement != null)
                {
                    sr.sprite = replacement;
                    replaced++;
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
                    replaced++;
                }
            }
        }

        return replaced;
    }

    private int ReplaceRenderers(AssetSwapManager manager)
    {
        var renderers = FindObjectsOfType<Renderer>();
        int replaced = 0;

        foreach (var r in renderers)
        {
            if (r == null || !r.gameObject.activeInHierarchy) continue;
            if (r is SpriteRenderer) continue;

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
                    replaced++;
                }
            }
        }

        return replaced;
    }
}
