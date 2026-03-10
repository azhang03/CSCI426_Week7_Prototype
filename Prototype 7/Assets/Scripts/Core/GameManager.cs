using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton managing top-level game state: Playing, Dead, and LevelComplete.
///
/// Death triggers a soft respawn: the player's melt animation plays, then
/// the player is teleported to the last active checkpoint (or the initial
/// spawn position) and health is restored.
///
/// Level complete triggers a fade-to-black with optional audio, then reloads.
/// Pressing R at any time hard-resets the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Dead, LevelComplete }
    public GameState State { get; private set; } = GameState.Playing;

    [Header("Respawn Timing")]
    [Tooltip("Seconds to wait after the melt animation before the player reappears.")]
    public float respawnPause = 0.3f;

    // Cached player references resolved once in Start.
    private GameObject playerObject;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private DeathEffect deathEffect;
    private SpriteRenderer playerSprite;
    private SpriteRenderer playerOverlay;

    private Vector2 initialSpawnPosition;
    private Vector2? activeCheckpoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
            playerHealth = playerObject.GetComponent<PlayerHealth>();
            deathEffect = playerObject.GetComponent<DeathEffect>();
            playerSprite = playerObject.GetComponent<SpriteRenderer>();
            playerOverlay = playerHealth != null ? playerHealth.redOverlayRenderer : null;
            initialSpawnPosition = playerObject.transform.position;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ReloadScene();
    }

    // ─────────────────────────────────────────
    // Checkpoint
    // ─────────────────────────────────────────

    public void SetCheckpoint(Vector2 position)
    {
        activeCheckpoint = position;
        Debug.Log($"[GameManager] Checkpoint set at {position}");
    }

    // ─────────────────────────────────────────
    // Death  →  soft respawn
    // ─────────────────────────────────────────

    public void OnPlayerDeath()
    {
        if (State != GameState.Playing) return;

        State = GameState.Dead;
        Debug.Log("[GameManager] Player died.");
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // Hide sprites before melt animation spawns particles.
        if (playerSprite != null) playerSprite.enabled = false;
        if (playerOverlay != null) playerOverlay.enabled = false;

        if (deathEffect != null)
        {
            bool animDone = false;
            deathEffect.Play(() => animDone = true);
            while (!animDone) yield return null;
        }

        yield return new WaitForSeconds(respawnPause);

        // Teleport to checkpoint or initial position.
        Vector2 respawnPos = activeCheckpoint ?? initialSpawnPosition;
        if (playerObject != null)
            playerObject.transform.position = (Vector3)respawnPos;

        // Restore player.
        if (playerSprite != null) playerSprite.enabled = true;
        if (playerOverlay != null) playerOverlay.enabled = true;
        if (playerHealth != null) playerHealth.ResetHealth();
        if (playerController != null) playerController.enabled = true;

        State = GameState.Playing;
    }

    // ─────────────────────────────────────────
    // Level complete  →  fade + audio + reload
    // ─────────────────────────────────────────

    public void OnLevelComplete(AudioClip clip, float volume, float fadeDuration,
        Sprite flySprite = null, Color? flyColor = null, float flyScale = 10f, float flyDuration = 2f)
    {
        if (State != GameState.Playing) return;

        State = GameState.LevelComplete;
        Debug.Log("[GameManager] Level complete!");

        FreezeAllEnemies();
        StartCoroutine(ExitSequence(clip, volume, fadeDuration, flySprite, flyColor ?? Color.white, flyScale, flyDuration));
    }

    private IEnumerator ExitSequence(AudioClip clip, float volume, float fadeDuration,
        Sprite flySprite, Color flyColor, float flyScale, float flyDuration)
    {
        // Hide player and freeze movement.
        if (playerSprite != null) playerSprite.enabled = false;
        if (playerOverlay != null) playerOverlay.enabled = false;
        if (playerController != null)
        {
            playerController.enabled = false;
            Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // Stop ambient audio so the exit clip plays cleanly.
        AmbientAudio ambient = FindFirstObjectByType<AmbientAudio>();
        if (ambient != null)
        {
            AudioSource ambientSource = ambient.GetComponent<AudioSource>();
            if (ambientSource != null) ambientSource.Stop();
        }

        // Build a full-screen black overlay.
        GameObject canvasGO = new GameObject("FadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        Image fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Start audio.
        AudioSource audioSource = null;
        float audioLength = 0f;
        if (clip != null)
        {
            audioSource = canvasGO.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
            audioLength = clip.length;
        }

        // Fade to black.
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeImage.color = Color.black;

        // Fly the sheep/puddle sprite across the black screen.
        if (flySprite != null)
        {
            GameObject flyGO = new GameObject("FlyingSprite");
            flyGO.transform.SetParent(canvasGO.transform, false);

            Image flyImage = flyGO.AddComponent<Image>();
            flyImage.sprite = flySprite;
            flyImage.color = flyColor;
            flyImage.preserveAspect = false;

            float pixelSize = 64f * flyScale;
            RectTransform flyRt = flyGO.GetComponent<RectTransform>();
            // Match the puddle's oval proportions (0.8 wide : 0.5 tall).
            flyRt.sizeDelta = new Vector2(pixelSize * 0.8f, pixelSize * 0.5f);
            flyRt.anchorMin = new Vector2(0.5f, 0.5f);
            flyRt.anchorMax = new Vector2(0.5f, 0.5f);
            flyRt.pivot = new Vector2(0.5f, 0.5f);

            float screenHalf = Screen.width * 0.5f + pixelSize * 0.8f;
            Vector2 startPos = new Vector2(screenHalf, 0f);
            Vector2 endPos = new Vector2(-screenHalf, 0f);

            float flyElapsed = 0f;
            while (flyElapsed < flyDuration)
            {
                flyElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(flyElapsed / flyDuration);
                flyRt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            Destroy(flyGO);
        }

        // Wait for audio to finish (if it extends beyond fade + fly).
        float totalAnimTime = fadeDuration + flyDuration;
        float remaining = audioLength - totalAnimTime;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        ReloadScene();
    }

    // ─────────────────────────────────────────
    // Timer expired  →  death animation + hard reset
    // ─────────────────────────────────────────

    public void OnTimerExpired()
    {
        if (State != GameState.Playing) return;

        State = GameState.Dead;
        Debug.Log("[GameManager] Timer expired.");
        StartCoroutine(TimerDeathSequence());
    }

    private IEnumerator TimerDeathSequence()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        if (playerSprite != null) playerSprite.enabled = false;
        if (playerOverlay != null) playerOverlay.enabled = false;

        if (deathEffect != null)
        {
            bool animDone = false;
            deathEffect.Play(() => animDone = true);
            while (!animDone) yield return null;
        }

        yield return new WaitForSeconds(respawnPause);

        ReloadScene();
    }

    // ─────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────

    private void FreezeAllEnemies()
    {
        foreach (EnemyController ec in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
        {
            ec.enabled = false;
            Rigidbody2D rb = ec.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
            }
        }

        foreach (Sheep s in FindObjectsByType<Sheep>(FindObjectsSortMode.None))
        {
            s.enabled = false;
            Rigidbody2D rb = s.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        foreach (FlashlightCone fc in FindObjectsByType<FlashlightCone>(FindObjectsSortMode.None))
            fc.enabled = false;

        foreach (LightZone lz in FindObjectsByType<LightZone>(FindObjectsSortMode.None))
            lz.enabled = false;

        Debug.Log("[GameManager] All enemies/sheep frozen, all damage sources disabled.");
    }

    private void ReloadScene()
    {
        StopAllCoroutines();
        State = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
