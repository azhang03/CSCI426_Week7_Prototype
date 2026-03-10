using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A circular "moon" countdown timer anchored to the top-left corner.
/// Starts at a bright color and gradually dims to grey as time runs out.
///
/// When the timer reaches zero, the player dies (via GameManager).
///
/// Assign a circular sprite (e.g. Circle.png) to moonSprite in the Inspector.
/// Attach this to any persistent GameObject (e.g. the GameManager object).
/// </summary>
public class MoonClock : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("Total duration of the countdown in seconds.")]
    public float duration = 120f;

    [Header("Appearance")]
    [Tooltip("Circular sprite to use for the clock. Assign Circle.png.")]
    public Sprite moonSprite;

    [Tooltip("Starting color of the moon (full time remaining).")]
    public Color fullColor = new Color(0.95f, 0.95f, 0.75f, 1f);

    [Tooltip("Color the moon dims to when time runs out.")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Warning Flash")]
    [Tooltip("Seconds remaining when the moon starts flashing red.")]
    public float warningThreshold = 10f;

    [Tooltip("How many times per second the moon flashes during the warning phase.")]
    public float flashSpeed = 3f;

    [Tooltip("Size of the clock icon in the UI (pixels).")]
    public float iconSize = 60f;

    [Tooltip("Padding from the screen edge (pixels).")]
    public float edgePadding = 16f;

    private Image moonImage;
    private float remaining;
    private bool running = false;

    private void Start()
    {
        remaining = duration;
        running = true;
        BuildUI();
    }

    private void Update()
    {
        if (!running) return;

        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
        {
            running = false;
            return;
        }

        remaining -= Time.deltaTime;

        if (moonImage != null)
        {
            if (remaining > warningThreshold)
            {
                // Dim from fullColor to emptyColor over the non-warning portion.
                float dimFraction = Mathf.Clamp01((remaining - warningThreshold) / (duration - warningThreshold));
                moonImage.color = Color.Lerp(emptyColor, fullColor, dimFraction);
            }
            else if (remaining > 0f)
            {
                // Already fully grey — flash between grey and red.
                float flash = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed * Mathf.PI));
                moonImage.color = Color.Lerp(emptyColor, Color.red, flash);
            }
        }

        if (remaining <= 0f)
        {
            running = false;
            if (moonImage != null)
                moonImage.color = Color.red;

            // Play death animation, then hard-reset the scene.
            if (GameManager.Instance != null)
                GameManager.Instance.OnTimerExpired();
        }
    }

    /// <summary>
    /// Resets the timer to full. Called externally if needed (e.g. on respawn).
    /// </summary>
    public void ResetTimer()
    {
        remaining = duration;
        running = true;

        if (moonImage != null)
            moonImage.color = fullColor;
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("MoonClockCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 91;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject iconGO = new GameObject("MoonIcon");
        iconGO.transform.SetParent(canvasGO.transform, false);

        moonImage = iconGO.AddComponent<Image>();
        moonImage.sprite = moonSprite;
        moonImage.color = fullColor;
        moonImage.preserveAspect = true;

        RectTransform rt = iconGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(edgePadding, -edgePadding);
        rt.sizeDelta = new Vector2(iconSize, iconSize);
    }
}
