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
/// The PolygonCollider2D points should be shaped to approximate the Light2D
/// Spot cone — set the inner/outer radius and angle on the Light2D, then
/// manually match the collider shape in the Inspector.
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

    // Cached reference — resolved once on Start via tag.
    private PlayerHealth playerHealth;
    // True when the player is inside the trigger collider AND has clear LoS.
    private bool playerInSight = false;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerHealth = playerObj.GetComponent<PlayerHealth>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerHealth == null) return;

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

        if (playerInSight)
        {
            playerInSight = false;
            playerHealth?.UnregisterLightHit();
        }
    }

    private void OnDisable()
    {
        if (playerInSight && playerHealth != null)
        {
            playerInSight = false;
            playerHealth.UnregisterLightHit();
        }
    }

    /// <summary>
    /// Returns true if there is no Barrier between this flashlight's root and the target position.
    /// The ray originates from this object's parent (the enemy root) so walls are tested from
    /// the correct source position.
    /// </summary>
    private bool HasLineOfSight(Vector2 targetPosition)
    {
        Vector2 origin = transform.parent != null ? (Vector2)transform.parent.position : (Vector2)transform.position;
        Vector2 direction = targetPosition - origin;
        float distance = direction.magnitude;

        // Cast only against Barrier layer — ignore everything else.
        RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, barrierLayerMask);
        return hit.collider == null;
    }
}
