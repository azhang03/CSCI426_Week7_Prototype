using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays greyscale icons for every Collectible in the scene at the
/// top-right of the screen. When a collectible is picked up, its icon
/// is removed from the UI.
///
/// Uses RawImage + pixel-level luminance conversion to produce true
/// greyscale regardless of the sprite's original colors.
///
/// IMPORTANT: Each collectible's sprite must have Read/Write Enabled
/// checked in its Texture Import Settings in Unity, otherwise
/// GetPixels() will throw an error at runtime.
///
/// Creates its own Canvas and layout programmatically — no manual Unity
/// UI setup required. Just attach this script to any persistent GameObject
/// (e.g. the GameManager object).
/// </summary>
public class CollectiblesUI : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Size of each collectible icon in the UI (pixels).")]
    public float iconSize = 40f;

    [Tooltip("Spacing between icons (pixels).")]
    public float iconSpacing = 8f;

    [Tooltip("Padding from the screen edge (pixels).")]
    public float edgePadding = 16f;

    private Dictionary<string, GameObject> iconsByID = new Dictionary<string, GameObject>();
    private List<GameObject> allIcons = new List<GameObject>();
    private Transform iconContainer;

    private void Start()
    {
        BuildCanvas();
        PopulateIcons();
    }

    private void OnEnable()
    {
        Collectible.OnCollected += HandleCollected;
    }

    private void OnDisable()
    {
        Collectible.OnCollected -= HandleCollected;
    }

    private void BuildCanvas()
    {
        GameObject canvasGO = new GameObject("CollectiblesCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject container = new GameObject("IconContainer");
        container.transform.SetParent(canvasGO.transform, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-edgePadding, -edgePadding);

        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = iconSpacing;
        hlg.childAlignment = TextAnchor.UpperRight;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        iconContainer = container.transform;
    }

    private void PopulateIcons()
    {
        Collectible[] collectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);

        foreach (Collectible c in collectibles)
        {
            SpriteRenderer sr = c.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            GameObject iconGO = new GameObject("Icon_" + c.collectibleID);
            iconGO.transform.SetParent(iconContainer, false);

            RawImage img = iconGO.AddComponent<RawImage>();
            img.texture = ToGreyscale(sr.sprite);
            img.color = Color.white;

            RectTransform iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(iconSize, iconSize);

            LayoutElement le = iconGO.AddComponent<LayoutElement>();
            le.preferredWidth = iconSize;
            le.preferredHeight = iconSize;

            string id = c.collectibleID;
            if (!string.IsNullOrEmpty(id))
                iconsByID[id] = iconGO;

            allIcons.Add(iconGO);
        }
    }

    /// <summary>
    /// Converts a sprite's texture to a greyscale Texture2D using luminance weights.
    /// Requires the source sprite's texture to have Read/Write enabled in Import Settings.
    /// </summary>
    private Texture2D ToGreyscale(Sprite sprite)
    {
        Texture2D src = sprite.texture;

        // Determine the pixel rect within the atlas (handles packed sprites too).
        int x = Mathf.RoundToInt(sprite.textureRect.x);
        int y = Mathf.RoundToInt(sprite.textureRect.y);
        int w = Mathf.RoundToInt(sprite.textureRect.width);
        int h = Mathf.RoundToInt(sprite.textureRect.height);

        Color[] pixels;
        try
        {
            pixels = src.GetPixels(x, y, w, h);
        }
        catch
        {
            // Fallback: return a plain grey texture if read/write is not enabled.
            Texture2D fallback = new Texture2D(1, 1);
            fallback.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 1f));
            fallback.Apply();
            Debug.LogWarning($"[CollectiblesUI] Sprite '{sprite.name}' texture is not readable. " +
                             "Enable Read/Write in the sprite's Import Settings.", this);
            return fallback;
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            float lum = pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f;
            pixels[i] = new Color(lum, lum, lum, pixels[i].a);
        }

        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    private void HandleCollected(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        if (iconsByID.TryGetValue(id, out GameObject iconGO))
        {
            iconsByID.Remove(id);
            allIcons.Remove(iconGO);
            Destroy(iconGO);
        }
    }
}
