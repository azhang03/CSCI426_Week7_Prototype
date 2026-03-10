using UnityEngine;

/// <summary>
/// Handles damage detection for the enemy's flashlight cone.
///
/// Attach this to the Flashlight child GameObject alongside Light2D and
/// PolygonCollider2D (set as trigger). When the player enters the cone trigger,
/// a Physics2D raycast is cast from the enemy toward the player on every frame.
/// If a Barrier-layer object blocks the ray, the player is in shadow and takes
/// no damage. Otherwise, damage is applied continuously (damage per second).
///
/// Automatically stops dealing damage when the game is not in the Playing state.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class FlashlightCone : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Health points removed per second while the player has clear line-of-sight inside the cone.")]
    public float damagePerSecond = 20f;

    [Header("Line-of-Sight")]
    [Tooltip("Physics layer mask for Barrier objects. Raycasts check only this layer.")]
    public LayerMask barrierLayerMask;

    private PlayerHealth playerHealth;
    private Transform playerTransform;
    private bool playerInSight = false;
    // True when the player's collider is inside the cone trigger (regardless of LoS).
    private bool playerInCone = false;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            playerTransform = playerObj.transform;
        }
    }

    private void LateUpdate()
    {
        if (playerInCone && playerTransform != null)
        {
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f;
            float parentAngle = transform.parent != null ? transform.parent.eulerAngles.z : 0f;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle - parentAngle);
        }
        else
        {
            transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>Returns true if the game is still active.</summary>
    private bool IsGameActive()
    {
        return GameManager.Instance == null || GameManager.Instance.State == GameManager.GameState.Playing;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerHealth == null) return;

        playerInCone = true;

        if (!IsGameActive())
        {
            if (playerInSight)
            {
                playerInSight = false;
                playerHealth.UnregisterLightHit();
            }
            return;
        }

        bool hasLoS = HasLineOfSight(other.transform.position);

        if (hasLoS)
        {
            if (!playerInSight)
            {
                playerInSight = true;
                playerHealth.RegisterLightHit();
            }
            playerHealth.TakeDamage(damagePerSecond * Time.deltaTime);
        }
        else
        {
            if (playerInSight)
            {
                playerInSight = false;
                playerHealth.UnregisterLightHit();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInCone = false;

        if (playerInSight)
        {
            playerInSight = false;
            playerHealth?.UnregisterLightHit();
        }
    }

    private void OnDisable()
    {
        playerInCone = false;

        if (playerInSight && playerHealth != null)
        {
            playerInSight = false;
            playerHealth.UnregisterLightHit();
        }
    }

    /// <summary>
    /// Returns true if there is no Barrier between this flashlight's root and the target position.
    /// </summary>
    private bool HasLineOfSight(Vector2 targetPosition)
    {
        Vector2 origin = transform.parent != null ? (Vector2)transform.parent.position : (Vector2)transform.position;
        Vector2 direction = targetPosition - origin;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, barrierLayerMask);
        return hit.collider == null;
    }
}
