using UnityEngine;

/// <summary>
/// Place on a trigger collider at the level exit.
/// When the Player enters, the player disappears and the screen fades to
/// black while an audio clip plays. After the audio ends, the scene reloads.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitPoint : MonoBehaviour
{
    [Header("Exit Sequence")]
    [Tooltip("Audio clip to play during the fade-out. Leave empty for silent fade.")]
    public AudioClip exitAudio;

    [Range(0f, 1f)]
    [Tooltip("Volume of the exit audio.")]
    public float audioVolume = 1f;

    [Tooltip("Seconds it takes for the screen to fade fully to black.")]
    public float fadeDuration = 2f;

    [Header("Flying Sheep")]
    [Tooltip("Sheep sprite shown flying across the black screen. Assign sheep.png.")]
    public Sprite sheepFlySprite;

    [Tooltip("Puddle sprite shown if the sheep was killed. Assign Circle.png.")]
    public Sprite puddleFlySprite;

    [Tooltip("Scale multiplier for the flying sprite.")]
    public float flyScale = 10f;

    [Tooltip("Seconds it takes to fly across the screen.")]
    public float flyDuration = 2f;

    private bool triggered = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;

        triggered = true;

        bool sheepWasKilled = GameObject.Find("SheepPuddle") != null;
        Sprite flySprite = sheepWasKilled ? puddleFlySprite : sheepFlySprite;
        Color flyColor = sheepWasKilled ? new Color(0.4f, 0f, 0f, 1f) : Color.white;

        GameManager.Instance.OnLevelComplete(
            exitAudio, audioVolume, fadeDuration,
            flySprite, flyColor, flyScale, flyDuration);
    }
}
