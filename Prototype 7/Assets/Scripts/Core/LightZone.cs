using UnityEngine;

/// <summary>
/// A rectangular area of static light that damages the player while they
/// stand in it. Attach to a GameObject with a BoxCollider2D (trigger).
///
/// Scale the GameObject's transform to change the size/aspect ratio of
/// the light strip. The BoxCollider2D and any child Light2D will scale
/// with the transform, keeping the damage zone and visuals matched.
///
/// Unlike FlashlightCone, this does NOT raycast for barriers — the visual
/// Light2D handles shadow casting via ShadowCaster2D, and if the player
/// is physically inside the trigger, they take damage. Barriers block
/// the player from entering the lit area anyway since they have solid
/// colliders. If you need barrier-aware damage, add a barrierLayerMask
/// field and a raycast check similar to FlashlightCone.
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerHealth == null) return;

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
