using UnityEngine;

/// <summary>
/// Manages the player's health and the red silhouette damage indicator.
///
/// The red silhouette works by placing a child GameObject (RedOverlay) on the
/// player with a duplicate SpriteRenderer using an unlit material. Its alpha
/// scales from 0 (full health) to 1 (dead), giving a gradually solidifying
/// red silhouette effect that is visible even in darkness.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Maximum health points.")]
    public float maxHealth = 100f;

    [Tooltip("Health regenerated per second when NOT in light.")]
    public float regenPerSecond = 5f;

    [Header("Red Overlay")]
    [Tooltip("The child SpriteRenderer used for the red silhouette overlay. Assign the RedOverlay child object here.")]
    public SpriteRenderer redOverlayRenderer;

    public float CurrentHealth { get; private set; }

    // Tracks how many flashlight cones currently have line-of-sight on the player.
    // Incremented by FlashlightCone, decremented when exiting or blocked.
    private int lightSourcesHitting = 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        if (redOverlayRenderer != null)
        {
            Color c = redOverlayRenderer.color;
            c.a = 0f;
            redOverlayRenderer.color = c;
        }
    }

    private void Update()
    {
        if (lightSourcesHitting <= 0)
            Regenerate();

        UpdateOverlay();
    }

    /// <summary>Called by FlashlightCone every frame while the player is in line-of-sight.</summary>
    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
            GameManager.Instance.OnPlayerDeath();
    }

    /// <summary>Called by FlashlightCone when it gains clear line-of-sight to the player.</summary>
    public void RegisterLightHit()
    {
        lightSourcesHitting++;
    }

    /// <summary>Called by FlashlightCone when it loses line-of-sight to the player.</summary>
    public void UnregisterLightHit()
    {
        lightSourcesHitting = Mathf.Max(0, lightSourcesHitting - 1);
    }

    private void Regenerate()
    {
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + regenPerSecond * Time.deltaTime);
    }

    private void UpdateOverlay()
    {
        if (redOverlayRenderer == null) return;

        float damageFraction = 1f - (CurrentHealth / maxHealth);
        Color c = redOverlayRenderer.color;
        c.a = damageFraction;
        redOverlayRenderer.color = c;
    }
}
