using UnityEngine;

/// <summary>
/// Manages the player's health and the red silhouette damage indicator.
///
/// The red silhouette works by placing a child GameObject (RedOverlay) on the
/// player with a duplicate SpriteRenderer using an unlit material. Its alpha
/// scales from 0 (full health) to 1 (dead), giving a gradually solidifying
/// red silhouette effect that is visible even in darkness.
///
/// Audio: a looping damage clip plays while any light source has line-of-sight,
/// and a one-shot death clip plays when health hits zero.
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

    [Header("Audio")]
    [Tooltip("Looping clip that plays while the player is being hit by light. Stops when they leave the light.")]
    public AudioClip damageSoundClip;

    [Range(0f, 1f)]
    [Tooltip("Volume of the continuous damage sound.")]
    public float damageSoundVolume = 1f;

    [Tooltip("One-shot clip that plays when the player dies.")]
    public AudioClip deathSoundClip;

    [Range(0f, 1f)]
    [Tooltip("Volume of the death sound.")]
    public float deathSoundVolume = 1f;

    public float CurrentHealth { get; private set; }

    private int lightSourcesHitting = 0;
    private AudioSource damageAudioSource;

    private void Awake()
    {
        CurrentHealth = maxHealth;

        if (redOverlayRenderer != null)
        {
            Color c = redOverlayRenderer.color;
            c.a = 0f;
            redOverlayRenderer.color = c;
        }

        damageAudioSource = gameObject.AddComponent<AudioSource>();
        damageAudioSource.playOnAwake = false;
        damageAudioSource.loop = true;
        damageAudioSource.spatialBlend = 0f;
        damageAudioSource.clip = damageSoundClip;
        damageAudioSource.volume = damageSoundVolume;
    }

    private void Update()
    {
        if (lightSourcesHitting <= 0)
            Regenerate();

        UpdateOverlay();

        // Keep volume in sync in case the slider is adjusted at runtime.
        damageAudioSource.volume = damageSoundVolume;
    }

    /// <summary>Called by FlashlightCone every frame while the player is in line-of-sight.</summary>
    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
        {
            StopDamageAudio();
            if (deathSoundClip != null)
                AudioSource.PlayClipAtPoint(deathSoundClip, transform.position, deathSoundVolume);
            GameManager.Instance.OnPlayerDeath();
        }
    }

    /// <summary>Called by FlashlightCone when it gains clear line-of-sight to the player.</summary>
    public void RegisterLightHit()
    {
        lightSourcesHitting++;

        if (lightSourcesHitting == 1)
            StartDamageAudio();
    }

    /// <summary>Called by FlashlightCone when it loses line-of-sight to the player.</summary>
    public void UnregisterLightHit()
    {
        lightSourcesHitting = Mathf.Max(0, lightSourcesHitting - 1);

        if (lightSourcesHitting == 0)
            StopDamageAudio();
    }

    /// <summary>
    /// Resets health to full, clears all active light-hit tracking, and
    /// updates the overlay. Called by GameManager on soft respawn.
    /// </summary>
    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        lightSourcesHitting = 0;
        StopDamageAudio();
        UpdateOverlay();
    }

    private void StartDamageAudio()
    {
        if (damageSoundClip == null) return;
        damageAudioSource.clip = damageSoundClip;
        damageAudioSource.volume = damageSoundVolume;
        damageAudioSource.Play();
    }

    private void StopDamageAudio()
    {
        if (damageAudioSource.isPlaying)
            damageAudioSource.Stop();
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
