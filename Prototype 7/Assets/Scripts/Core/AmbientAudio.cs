using UnityEngine;

/// <summary>
/// Plays a looping ambient audio clip for the duration of the scene.
/// Attach to any persistent GameObject (e.g. the GameManager object).
/// </summary>
public class AmbientAudio : MonoBehaviour
{
    [Tooltip("Looping audio clip to play as background ambience.")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("Volume of the ambient audio.")]
    public float volume = 0.5f;

    private void Start()
    {
        if (clip == null) return;

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.Play();
    }
}
