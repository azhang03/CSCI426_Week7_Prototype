using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton managing top-level game state: Playing, Dead, and LevelComplete.
///
/// On death the scene is reloaded after a short delay so the player can see
/// the fully red silhouette for a moment before reset.
/// On level complete the scene reloads as a placeholder — replace with a proper
/// scene transition or UI screen when ready.
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
        Invoke(nameof(ReloadScene), levelCompleteReloadDelay);
    }

    private void ReloadScene()
    {
        State = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
