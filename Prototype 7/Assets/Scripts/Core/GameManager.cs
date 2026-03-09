using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton managing top-level game state: Playing, Dead, and LevelComplete.
///
/// On death the scene is reloaded after a short delay so the player can see
/// the fully red silhouette for a moment before reset.
/// On level complete all enemies freeze in place so the player feels safe,
/// then the scene reloads after a delay.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Dead, LevelComplete }
    public GameState State { get; private set; } = GameState.Playing;

    [Header("Timing")]
    [Tooltip("Seconds to wait after death before reloading the scene.")]
    public float deathReloadDelay = 1.5f;

    [Tooltip("Seconds to wait after level complete before reloading the scene.")]
    public float levelCompleteReloadDelay = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Called by PlayerHealth when health reaches zero.</summary>
    public void OnPlayerDeath()
    {
        if (State != GameState.Playing) return;

        State = GameState.Dead;
        Debug.Log("[GameManager] Player died. Reloading scene...");
        Invoke(nameof(ReloadScene), deathReloadDelay);
    }

    /// <summary>Called by ExitPoint when the player reaches the exit.</summary>
    public void OnLevelComplete()
    {
        if (State != GameState.Playing) return;

        State = GameState.LevelComplete;
        Debug.Log("[GameManager] Level complete!");

        FreezeAllEnemies();

        Invoke(nameof(ReloadScene), levelCompleteReloadDelay);
    }

    /// <summary>
    /// Disables every EnemyController in the scene and zeroes their velocity
    /// so they stop mid-patrol. Also disables all FlashlightCone and LightZone
    /// damage so the player doesn't take hits after reaching the exit.
    /// </summary>
    private void FreezeAllEnemies()
    {
        // Freeze patrol enemies.
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

        // Disable all flashlight cone damage scripts.
        foreach (FlashlightCone fc in FindObjectsByType<FlashlightCone>(FindObjectsSortMode.None))
        {
            fc.enabled = false;
        }

        // Disable all static light zone damage scripts.
        foreach (LightZone lz in FindObjectsByType<LightZone>(FindObjectsSortMode.None))
        {
            lz.enabled = false;
        }

        Debug.Log("[GameManager] All enemies frozen, all damage sources disabled.");
    }

    private void ReloadScene()
    {
        State = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
