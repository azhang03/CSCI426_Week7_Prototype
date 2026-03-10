using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A rectangular area of static light that damages the player while they
/// stand in it. Attach to the same GameObject as a Light2D component.
///
/// The BoxCollider2D automatically resizes to match the Light2D's local
/// bounds, so you only need to edit the Light2D shape — the damage zone
/// follows automatically.
///
/// Optional features (configured in Inspector):
///   - Collectible unlock: set requiredCollectibleID to gate this zone
///     behind a specific Collectible pickup.
///   - Strobe: toggle isStrobing to make the light flash on/off.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Light2D))]
public class LightZone : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Health points removed per second while the player is inside this light zone.")]
    public float damagePerSecond = 15f;

    [Header("Collectible Unlock")]
    [Tooltip("If non-empty, this zone stays active until the Collectible with a matching ID is picked up, then it permanently turns off.")]
    public string requiredCollectibleID = "";

    [Header("Strobe")]
    [Tooltip("When true, the light flashes on and off.")]
    public bool isStrobing = false;

    [Tooltip("Seconds the light stays ON during a strobe cycle.")]
    public float strobeOnTime = 1f;

    [Tooltip("Seconds the light stays OFF during a strobe cycle.")]
    public float strobeOffTime = 1f;

    private PlayerHealth playerHealth;
    private bool playerInZone = false;
    private BoxCollider2D boxCollider;
    private Light2D light2D;

    private float strobeTimer;
    private bool strobeOn = true;
    private bool unlocked = false; // true = permanently disabled by collectible

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        light2D = GetComponent<Light2D>();

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        SyncColliderToLight();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerHealth = playerObj.GetComponent<PlayerHealth>();

        strobeTimer = 0f;
        strobeOn = true;
    }

    private void OnEnable()
    {
        Collectible.OnCollected += HandleCollectibleCollected;
    }

    private void OnDisable()
    {
        Collectible.OnCollected -= HandleCollectibleCollected;

        if (playerInZone && playerHealth != null)
        {
            playerInZone = false;
            playerHealth.UnregisterLightHit();
        }
    }

    private void Update()
    {
        SyncColliderToLight();

        if (unlocked) return;

        if (isStrobing)
            UpdateStrobe();
    }

    private void UpdateStrobe()
    {
        strobeTimer += Time.deltaTime;

        if (strobeOn)
        {
            if (strobeTimer >= strobeOnTime)
            {
                strobeTimer = 0f;
                SetStrobeState(false);
            }
        }
        else
        {
            if (strobeTimer >= strobeOffTime)
            {
                strobeTimer = 0f;
                SetStrobeState(true);
            }
        }
    }

    private void SetStrobeState(bool on)
    {
        strobeOn = on;

        if (light2D != null) light2D.enabled = on;
        if (boxCollider != null) boxCollider.enabled = on;

        if (!on && playerInZone && playerHealth != null)
        {
            playerInZone = false;
            playerHealth.UnregisterLightHit();
        }
    }

    private void HandleCollectibleCollected(string id)
    {
        if (string.IsNullOrEmpty(requiredCollectibleID)) return;
        if (id != requiredCollectibleID) return;

        unlocked = true;
        if (light2D != null) light2D.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;

        if (playerInZone && playerHealth != null)
        {
            playerInZone = false;
            playerHealth.UnregisterLightHit();
        }
    }

    private void SyncColliderToLight()
    {
        if (light2D == null || boxCollider == null) return;

        Vector3[] path = light2D.shapePath;
        if (path == null || path.Length == 0) return;

        Vector2 min = path[0];
        Vector2 max = path[0];

        for (int i = 1; i < path.Length; i++)
        {
            min = Vector2.Min(min, path[i]);
            max = Vector2.Max(max, path[i]);
        }

        boxCollider.offset = (min + max) * 0.5f;
        boxCollider.size = max - min;
    }

    private bool IsGameActive()
    {
        return GameManager.Instance == null || GameManager.Instance.State == GameManager.GameState.Playing;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerHealth == null) return;

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
}
