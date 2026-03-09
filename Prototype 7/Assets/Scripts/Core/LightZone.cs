using UnityEngine;

/// <summary>
/// A rectangular area of static light that damages the player while they
/// stand in it. Attach to a GameObject with a BoxCollider2D (trigger).
///
/// Automatically stops dealing damage when the game is not in the Playing state.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class LightZone : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Health points removed per second while the player is inside this light zone.")]
    public float damagePerSecond = 15f;

    private PlayerHealth playerHealth;
    private bool playerInZone = false;

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerHealth = playerObj.GetComponent<PlayerHealth>();
    }

    private bool IsGameActive()
    {
        return GameManager.Instance == null || GameManager.Instance.State == GameManager.GameState.Playing;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerHealth == null) return;

        // Stop all damage when the game is over.
        if (!IsGameActive())
        {
            if (playerInZone)
            {
                playerInZone = false;
                playerHealth.UnregisterLightHit();
            }
            return;
        }

        if (!playerInZone)
        {
            playerInZone = true;
            playerHealth.RegisterLightHit();
        }

        playerHealth.TakeDamage(damagePerSecond * Time.deltaTime);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerInZone)
        {
            playerInZone = false;
            playerHealth?.UnregisterLightHit();
        }
    }

    private void OnDisable()
    {
        if (playerInZone && playerHealth != null)
        {
            playerInZone = false;
            playerHealth.UnregisterLightHit();
        }
    }
}
