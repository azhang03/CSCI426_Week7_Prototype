using UnityEngine;

/// <summary>
/// Plays an audio clip once when the player enters the trigger collider.
/// Optionally repeats every time the player re-enters.
///
/// Prefab setup: any Collider2D (set as trigger), this script.
/// The collider size/shape defines the audio trigger area.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AudioZone : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("The audio clip to play when the player enters.")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("Playback volume.")]
    public float volume = 1f;

    [Tooltip("If true, the clip plays every time the player enters. If false, only the first time.")]
    public bool repeatable = false;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (clip == null) return;
        if (!repeatable && hasPlayed) return;

        audioSource.volume = volume;
        audioSource.PlayOneShot(clip);
        hasPlayed = true;
    }
}
