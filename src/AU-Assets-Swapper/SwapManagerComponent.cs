using System;
using System.Collections.Generic;
using UnityEngine;

namespace AU_Assets_Swapper;

public class SwapManagerComponent : MonoBehaviour
{
    private string _lastScene = "";

    private bool _pickMode;
    private int _lastPickedID;
    private float _nextPickTime;
    private const float PickInterval = 0.3f;
    private readonly List<string> _pickInfoLines = new();
    private GUIStyle _pickBoxStyle;
    private GUIStyle _pickLabelStyle;
    private GUIStyle _pickHeaderStyle;

    private readonly List<SpriteRenderer> _spriteRendererCache = new();
    private readonly List<UnityEngine.UI.Image> _imageCache = new();
    private readonly List<UnityEngine.UI.Text> _textCache = new();
    private readonly List<Renderer> _rendererCache = new();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Plugin.SwapManager?.Rescan();
            ScanAndReplace();
            Plugin.LogSource.LogInfo("[AUAS] Assets rescanned (F5 pressed).");
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            _pickMode = !_pickMode;
            _lastPickedID = 0;
            _pickInfoLines.Clear();
            Plugin.LogSource.LogInfo(_pickMode
                ? "[AUAS] Pick mode ON - hover over game elements"
                : "[AUAS] Pick mode OFF");
        }

        if (_pickMode && Time.time >= _nextPickTime)
        {
            _nextPickTime = Time.time + PickInterval;
            InspectUnderMouse();
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene != _lastScene)
        {
            _lastScene = scene;
            Plugin.SwapManager?.Rescan();
            ScanAndReplace();
            Plugin.LogSource.LogInfo($"[AUAS] Scene changed to '{scene}', scanning assets");
        }
    }

    #region Asset Picker

    private void InspectUnderMouse()
    {
        try
        {
            var cam = Camera.main;
            if (cam == null) return;

            var mousePos = Input.mousePosition;
            var worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));

            var hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                var id = hit.gameObject.GetInstanceID();
                if (id != _lastPickedID)
                {
                    _lastPickedID = id;
                    LogGameObjectAssets(hit.gameObject, "2D");
                }
                return;
            }

            if (CheckUIUnderMouse(mousePos)) return;

            CheckRenderersUnderMouse(worldPos);
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Pick error: {ex.Message}");
        }
    }

    private bool CheckUIUnderMouse(Vector3 screenPos)
    {
        _imageCache.Clear();
        _imageCache.AddRange(FindObjectsOfType<UnityEngine.UI.Image>());
        foreach (var img in _imageCache)
        {
            if (img == null || !img.gameObject.activeInHierarchy) continue;
            var rect = img.rectTransform;
            if (rect == null) continue;

            var canvas = img.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam))
            {
                var id = img.gameObject.GetInstanceID();
                if (id != _lastPickedID)
                {
                    _lastPickedID = id;
                    LogGameObjectAssets(img.gameObject, "UI");
                }
                return true;
            }
        }

        _textCache.Clear();
        _textCache.AddRange(FindObjectsOfType<UnityEngine.UI.Text>());
        foreach (var txt in _textCache)
        {
            if (txt == null || !txt.gameObject.activeInHierarchy) continue;
            var rect = txt.rectTransform;
            if (rect == null) continue;

            var canvas = txt.GetComponentInParent<Canvas>();
            var cam = canvas != null ? canvas.worldCamera : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam))
            {
                var id = txt.gameObject.GetInstanceID();
                if (id != _lastPickedID)
                {
                    _lastPickedID = id;
                    LogGameObjectAssets(txt.gameObject, "UI Text");
                }
                return true;
            }
        }

        return false;
    }

    private void CheckRenderersUnderMouse(Vector3 worldPos)
    {
        _rendererCache.Clear();
        _rendererCache.AddRange(FindObjectsOfType<Renderer>());
        Renderer best = null;
        float bestArea = float.MaxValue;

        foreach (var r in _rendererCache)
        {
            if (r == null || !r.gameObject.activeInHierarchy) continue;

            var bounds = r.bounds;
            if (worldPos.x < bounds.min.x || worldPos.x > bounds.max.x ||
                worldPos.y < bounds.min.y || worldPos.y > bounds.max.y) continue;

            var size = bounds.size.x * bounds.size.y;
            if (size < bestArea)
            {
                bestArea = size;
                best = r;
            }
        }

        if (best != null)
        {
            var id = best.gameObject.GetInstanceID();
            if (id != _lastPickedID)
            {
                _lastPickedID = id;
                LogGameObjectAssets(best.gameObject, "Renderer");
            }
        }
    }

    private void LogGameObjectAssets(GameObject go, string source)
    {
        _pickInfoLines.Clear();

        var path = GetHierarchyPath(go);
        _pickInfoLines.Add($"[{source}] {go.name}");
        _pickInfoLines.Add($"Path: {path}");

        var manager = Plugin.SwapManager;
        bool foundAny = false;

        var sprites = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprites)
        {
            if (sr == null || sr.sprite == null) continue;
            var spriteName = sr.sprite.name;
            var size = $"{sr.sprite.texture.width}x{sr.sprite.texture.height}";
            _pickInfoLines.Add($"  Sprite [{sr.gameObject.name}]: {spriteName} ({size})");
            if (manager != null && manager.HasSpriteReplacement(spriteName))
                _pickInfoLines.Add($"    >> HAS SPRITE REPLACEMENT");
            if (manager != null && manager.HasTextureReplacement(spriteName))
                _pickInfoLines.Add($"    >> HAS TEXTURE REPLACEMENT");
            foundAny = true;
        }

        var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
            if (img == null || img.sprite == null) continue;
            var spriteName = img.sprite.name;
            var size = $"{img.sprite.texture.width}x{img.sprite.texture.height}";
            _pickInfoLines.Add($"  UI Image [{img.gameObject.name}]: {spriteName} ({size})");
            if (manager != null && manager.HasSpriteReplacement(spriteName))
                _pickInfoLines.Add($"    >> HAS SPRITE REPLACEMENT");
            if (manager != null && manager.HasTextureReplacement(spriteName))
                _pickInfoLines.Add($"    >> HAS TEXTURE REPLACEMENT");
            foundAny = true;
        }

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (r is SpriteRenderer) continue;
            var mat = r.material;
            if (mat == null) continue;

            var texNames = mat.GetTexturePropertyNames();
            foreach (var propName in texNames)
            {
                var tex = mat.GetTexture(propName);
                if (tex == null) continue;
                var texSize = $"{tex.width}x{tex.height}";
                _pickInfoLines.Add($"  Texture [{r.gameObject.name}] {propName}: {tex.name} ({texSize})");
                if (manager != null && manager.HasTextureReplacement(tex.name))
                    _pickInfoLines.Add($"    >> HAS TEXTURE REPLACEMENT");
                foundAny = true;
            }

            if (mat.HasProperty("_Color"))
            {
                _pickInfoLines.Add($"  Color [{r.gameObject.name}]: {mat.color}");
                foundAny = true;
            }
        }

        var uiTexts = go.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        foreach (var txt in uiTexts)
        {
            if (txt == null) continue;
            var fontName = txt.font != null ? txt.font.name : "null";
            _pickInfoLines.Add($"  UI Text [{txt.gameObject.name}]: \"{txt.text}\" (Font: {fontName})");
            foundAny = true;
        }

        if (!foundAny)
        {
            _pickInfoLines.Add("  (no visual components found)");
        }

        foreach (var line in _pickInfoLines)
        {
            Plugin.LogSource.LogInfo($"[AUAS-PICK] {line}");
        }
    }

    private void OnGUI()
    {
        if (!_pickMode && _pickInfoLines.Count == 0) return;

        InitStyles();

        float x = 10f;
        float y = 10f;
        float boxWidth = 520f;
        float lineHeight = 20f;

        if (_pickMode)
        {
            var statusRect = new Rect(x, y, boxWidth, 28f);
            GUI.Box(statusRect, "", _pickBoxStyle);
            GUI.contentColor = Color.cyan;
            GUI.Label(new Rect(x + 8f, y + 4f, boxWidth - 16f, 22f),
                "AUAS Pick Mode: ON  |  F5=Rescan  F7=Toggle", _pickHeaderStyle);
            GUI.contentColor = Color.white;
            y += 34f;
        }

        if (_pickInfoLines.Count > 0)
        {
            float infoHeight = _pickInfoLines.Count * lineHeight + 16f;
            var infoRect = new Rect(x, y, boxWidth, infoHeight);
            GUI.Box(infoRect, "", _pickBoxStyle);

            float ly = y + 8f;
            foreach (var line in _pickInfoLines)
            {
                if (line.StartsWith("  >>"))
                    GUI.contentColor = Color.green;
                else if (line.StartsWith("["))
                    GUI.contentColor = Color.yellow;
                else if (line.StartsWith("Path:"))
                    GUI.contentColor = Color.gray;
                else
                    GUI.contentColor = Color.white;

                GUI.Label(new Rect(x + 8f, ly, boxWidth - 16f, lineHeight), line, _pickLabelStyle);
                ly += lineHeight;
            }
            GUI.contentColor = Color.white;
        }
    }

    private void InitStyles()
    {
        if (_pickBoxStyle != null) return;

        _pickBoxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f)) }
        };

        _pickLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal,
            normal = { textColor = Color.white },
            richText = true
        };

        _pickHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        var tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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

    #endregion

    #region Asset Scanner

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

    private int ReplaceSpriteRenderers(AssetSwapManager manager)
    {
        _spriteRendererCache.Clear();
        _spriteRendererCache.AddRange(FindObjectsOfType<SpriteRenderer>());
        int replaced = 0;

        foreach (var sr in _spriteRendererCache)
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
        _rendererCache.Clear();
        _rendererCache.AddRange(FindObjectsOfType<Renderer>());
        int replaced = 0;

        foreach (var r in _rendererCache)
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

    #endregion
}
