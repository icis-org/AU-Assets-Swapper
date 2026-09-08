using System;
using System.Collections.Generic;
using UnityEngine;

namespace AU_Assets_Swapper;

public class SwapManagerComponent : MonoBehaviour
{
    private float _nextScanTime;
    private const float ScanInterval = 0.2f;
    private const float RetryDuration = 3f;
    private float _retryUntil;
    private string _lastScene = "";

    private bool _pickMode;
    private readonly List<Collider2D> _overlapResults = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Plugin.SwapManager?.Rescan();
            Plugin.LogSource.LogInfo("[AUAS] Assets rescanned (F5 pressed).");
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            _pickMode = !_pickMode;
            Plugin.LogSource.LogInfo(_pickMode
                ? "[AUAS] Pick mode ON - hover over game elements"
                : "[AUAS] Pick mode OFF");
        }

        if (_pickMode)
        {
            InspectUnderMouse();
        }

        float now = Time.time;
        bool aggressiveScan = now < _retryUntil;

        if (!aggressiveScan && now < _nextScanTime) return;
        _nextScanTime = now + ScanInterval;

        ScanAndReplace();
    }

    private void InspectUnderMouse()
    {
        try
        {
            var cam = Camera.main;
            if (cam == null) return;

            var mousePos = Input.mousePosition;
            var worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

            _overlapResults.Clear();
            var count = Physics2D.OverlapPointNonAlloc(worldPos, _overlapResults.ToArray());
            var hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                LogGameObjectAssets(hit.gameObject, "2D");
                return;
            }

            CheckUIUnderMouse(mousePos);
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Pick error: {ex.Message}");
        }
    }

    private void CheckUIUnderMouse(Vector3 screenPos)
    {
        var images = FindObjectsOfType<UnityEngine.UI.Image>();
        foreach (var img in images)
        {
            if (img == null || !img.gameObject.activeInHierarchy) continue;
            var rect = img.rectTransform;
            if (rect == null) continue;

            var canvas = img.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam))
            {
                LogGameObjectAssets(img.gameObject, "UI");
                return;
            }
        }

        var texts = FindObjectsOfType<UnityEngine.UI.Text>();
        foreach (var txt in texts)
        {
            if (txt == null || !txt.gameObject.activeInHierarchy) continue;
            var rect = txt.rectTransform;
            if (rect == null) continue;

            var canvas = txt.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam))
            {
                LogGameObjectAssets(txt.gameObject, "UI");
                return;
            }
        }
    }

    private void LogGameObjectAssets(GameObject go, string source)
    {
        var path = GetHierarchyPath(go);
        Plugin.LogSource.LogInfo($"[AUAS-PICK] {source}: {go.name} (path: {path})");

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Plugin.LogSource.LogInfo($"  Sprite: {sr.sprite.name} ({sr.sprite.texture.width}x{sr.sprite.texture.height})");
        }

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            var mat = renderer.material;
            if (mat.HasProperty("_MainTex") && mat.mainTexture != null)
                Plugin.LogSource.LogInfo($"  Texture: {mat.mainTexture.name}");
            if (mat.HasProperty("_Color"))
                Plugin.LogSource.LogInfo($"  Color: {mat.color}");
        }

        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null && img.sprite != null)
        {
            Plugin.LogSource.LogInfo($"  UI Sprite: {img.sprite.name} ({img.sprite.texture.width}x{img.sprite.texture.height})");
        }

        var txt = go.GetComponent<UnityEngine.UI.Text>();
        if (txt != null)
        {
            Plugin.LogSource.LogInfo($"  UI Text: \"{txt.text}\", Font: {txt.font?.name ?? "null"}");
        }
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var parts = new List<string>();
        var t = go.transform;
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
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
