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

        var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != _lastScene)
        {
            _lastScene = sceneName;
            Plugin.SwapManager?.Rescan();
            ScanAndReplace();
            Plugin.LogSource.LogInfo($"[AUAS] Scene changed to '{sceneName}', scanning assets");
        }
    }

    private void InspectUnderMouse()
    {
        try
        {
            var camera = Camera.main;
            if (camera == null) return;

            var mousePosition = Input.mousePosition;
            var worldPosition = camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 0f));

            var hit = Physics2D.OverlapPoint(worldPosition);
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

            if (CheckUIUnderMouse(mousePosition)) return;

            CheckRenderersUnderMouse(worldPosition);
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogWarning($"[AUAS] Pick error: {ex.Message}");
        }
    }

    private bool CheckUIUnderMouse(Vector3 screenPos)
    {
        foreach (var img in FindObjectsOfType<UnityEngine.UI.Image>())
        {
            if (img == null || !img.gameObject.activeInHierarchy) continue;
            var canvas = img.GetComponentInParent<Canvas>();
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    img.rectTransform, screenPos, canvas?.worldCamera))
                return MarkPicked(img.gameObject, "UI");
        }

        foreach (var txt in FindObjectsOfType<UnityEngine.UI.Text>())
        {
            if (txt == null || !txt.gameObject.activeInHierarchy) continue;
            var canvas = txt.GetComponentInParent<Canvas>();
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    txt.rectTransform, screenPos, canvas?.worldCamera))
                return MarkPicked(txt.gameObject, "UI Text");
        }

        return false;
    }

    private bool MarkPicked(GameObject go, string source)
    {
        var id = go.GetInstanceID();
        if (id == _lastPickedID) return true;
        _lastPickedID = id;
        LogGameObjectAssets(go, source);
        return true;
    }

    private void CheckRenderersUnderMouse(Vector3 worldPos)
    {
        _rendererCache.Clear();
        _rendererCache.AddRange(FindObjectsOfType<Renderer>());
        Renderer bestMatch = null;
        float bestArea = float.MaxValue;

        foreach (var renderer in _rendererCache)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;

            var bounds = renderer.bounds;
            if (worldPos.x < bounds.min.x || worldPos.x > bounds.max.x ||
                worldPos.y < bounds.min.y || worldPos.y > bounds.max.y) continue;

            var area = bounds.size.x * bounds.size.y;
            if (area < bestArea)
            {
                bestArea = area;
                bestMatch = renderer;
            }
        }

        if (bestMatch != null)
        {
            var id = bestMatch.gameObject.GetInstanceID();
            if (id != _lastPickedID)
            {
                _lastPickedID = id;
                LogGameObjectAssets(bestMatch.gameObject, "Renderer");
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
        var foundAnyAsset = false;

        var sprites = go.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var spriteRenderer in sprites)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) continue;

            var spriteName = spriteRenderer.sprite.name;
            var size = $"{spriteRenderer.sprite.texture.width}x{spriteRenderer.sprite.texture.height}";
            _pickInfoLines.Add($"  Sprite [{spriteRenderer.gameObject.name}]: {spriteName} ({size})");
            if (manager != null && manager.HasSpriteReplacement(spriteName))
                _pickInfoLines.Add("    >> HAS SPRITE REPLACEMENT");
            if (manager != null && manager.HasTextureReplacement(spriteName))
                _pickInfoLines.Add("    >> HAS TEXTURE REPLACEMENT");
            foundAnyAsset = true;
        }

        var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var image in images)
        {
            if (image == null || image.sprite == null) continue;

            var spriteName = image.sprite.name;
            var size = $"{image.sprite.texture.width}x{image.sprite.texture.height}";
            _pickInfoLines.Add($"  UI Image [{image.gameObject.name}]: {spriteName} ({size})");
            if (manager != null && manager.HasSpriteReplacement(spriteName))
                _pickInfoLines.Add("    >> HAS SPRITE REPLACEMENT");
            if (manager != null && manager.HasTextureReplacement(spriteName))
                _pickInfoLines.Add("    >> HAS TEXTURE REPLACEMENT");
            foundAnyAsset = true;
        }

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (renderer is SpriteRenderer) continue;

            var material = renderer.material;
            if (material == null) continue;

            var textureNames = material.GetTexturePropertyNames();
            foreach (var propertyName in textureNames)
            {
                var texture = material.GetTexture(propertyName);
                if (texture == null) continue;

                var textureSize = $"{texture.width}x{texture.height}";
                _pickInfoLines.Add($"  Texture [{renderer.gameObject.name}] {propertyName}: {texture.name} ({textureSize})");
                if (manager != null && manager.HasTextureReplacement(texture.name))
                    _pickInfoLines.Add("    >> HAS TEXTURE REPLACEMENT");
                foundAnyAsset = true;
            }

            if (material.HasProperty("_Color"))
            {
                _pickInfoLines.Add($"  Color [{renderer.gameObject.name}]: {material.color}");
                foundAnyAsset = true;
            }
        }

        var uiTexts = go.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        foreach (var text in uiTexts)
        {
            if (text == null) continue;
            var fontName = text.font != null ? text.font.name : "null";
            _pickInfoLines.Add($"  UI Text [{text.gameObject.name}]: \"{text.text}\" (Font: {fontName})");
            foundAnyAsset = true;
        }

        if (!foundAnyAsset)
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

            float labelY = y + 8f;
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

                GUI.Label(new Rect(x + 8f, labelY, boxWidth - 16f, lineHeight), line, _pickLabelStyle);
                labelY += lineHeight;
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

        var texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static string GetHierarchyPath(GameObject go)
    {
        var pathParts = new List<string>();
        var current = go.transform;
        while (current != null)
        {
            pathParts.Add(current.name);
            current = current.parent;
        }

        pathParts.Reverse();
        return string.Join("/", pathParts);
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

    private int ReplaceSpriteRenderers(AssetSwapManager manager)
    {
        _spriteRendererCache.Clear();
        _spriteRendererCache.AddRange(FindObjectsOfType<SpriteRenderer>());

        var replacedCount = 0;
        foreach (var spriteRenderer in _spriteRendererCache)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) continue;
            if (!spriteRenderer.gameObject.activeInHierarchy) continue;

            var spriteName = spriteRenderer.sprite.name;
            if (manager.HasSpriteReplacement(spriteName))
            {
                var replacement = manager.LoadReplacementSprite(spriteName);
                if (replacement != null)
                {
                    spriteRenderer.sprite = replacement;
                    replacedCount++;
                }
            }
            else if (manager.HasTextureReplacement(spriteName))
            {
                var texture = manager.LoadReplacementTexture(spriteName);
                if (texture != null)
                {
                    var newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), spriteRenderer.sprite.pixelsPerUnit);
                    newSprite.name = spriteName;
                    spriteRenderer.sprite = newSprite;
                    replacedCount++;
                }
            }
        }

        return replacedCount;
    }

    private int ReplaceRenderers(AssetSwapManager manager)
    {
        _rendererCache.Clear();
        _rendererCache.AddRange(FindObjectsOfType<Renderer>());

        var replacedCount = 0;
        foreach (var renderer in _rendererCache)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy) continue;
            if (renderer is SpriteRenderer) continue;

            var material = renderer.material;
            if (material == null) continue;
            if (!material.HasProperty("_MainTex")) continue;
            if (material.mainTexture == null) continue;

            var textureName = material.mainTexture.name;
            if (manager.HasTextureReplacement(textureName))
            {
                var replacement = manager.LoadReplacementTexture(textureName);
                if (replacement != null)
                {
                    material.mainTexture = replacement;
                    replacedCount++;
                }
            }
        }

        return replacedCount;
    }
}
