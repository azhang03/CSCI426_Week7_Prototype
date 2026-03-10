using System.Collections;
using UnityEngine;

/// <summary>
/// A patrol-looping sheep that takes damage when the player touches it.
/// After 3 hits (with brief invincibility between each), it dies and
/// leaves behind a dark-red oval puddle.
///
/// Reuses the PatrolSegment struct defined alongside EnemyController.
///
/// Prefab setup: SpriteRenderer (sheep.png, Entities layer),
/// Rigidbody2D (Kinematic), BoxCollider2D (trigger), this script.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Sheep : MonoBehaviour
{
    [Header("Patrol Path")]
    [Tooltip("Ordered list of movement segments. Leave empty for a stationary sheep.")]
    public PatrolSegment[] patrolPath;

    [Tooltip("If true, the sheep loops back to segment 0 after completing all segments.")]
    public bool loopPath = true;

    [Header("Health")]
    [Tooltip("How many hits before the sheep dies.")]
    public int hitPoints = 3;

    [Tooltip("Seconds of invincibility after each hit.")]
    public float invincibilityDuration = 0.3f;

    [Header("Death")]
    [Tooltip("Sprite used for the puddle left behind on death. Assign Circle.png.")]
    public Sprite puddleSprite;

    [Header("Audio")]
    [Tooltip("Clip played when the sheep takes a hit.")]
    public AudioClip hitSound;

    [Range(0f, 1f)]
    [Tooltip("Volume of the hit sound.")]
    public float hitVolume = 1f;

    [Tooltip("Clip played when the sheep dies.")]
    public AudioClip deathSound;

    [Range(0f, 1f)]
    [Tooltip("Volume of the death sound.")]
    public float deathVolume = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private AudioSource audioSource;

    private int currentSegmentIndex = 0;
    private float segmentTimer = 0f;
    private Vector2 segmentStartPosition;

    private bool isInvincible = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;

        GetComponent<Collider2D>().isTrigger = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        segmentStartPosition = rb.position;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        UpdatePatrol();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isInvincible) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing) return;

        hitPoints--;

        if (hitPoints <= 0)
        {
            if (deathSound != null)
                AudioSource.PlayClipAtPoint(deathSound, transform.position, deathVolume);
            SpawnPuddle();
            Destroy(gameObject);
            return;
        }

        if (hitSound != null)
            audioSource.PlayOneShot(hitSound, hitVolume);

        StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        isInvincible = true;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(0.15f);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        yield return new WaitForSeconds(Mathf.Max(0f, invincibilityDuration - 0.15f));

        isInvincible = false;
    }

    private void SpawnPuddle()
    {
        if (puddleSprite == null) return;

        GameObject puddle = new GameObject("SheepPuddle");
        puddle.transform.position = transform.position;
        puddle.transform.localScale = new Vector3(0.8f, 0.5f, 1f);

        SpriteRenderer sr = puddle.AddComponent<SpriteRenderer>();
        sr.sprite = puddleSprite;
        sr.color = new Color(0.4f, 0f, 0f, 1f);
        sr.sortingLayerName = "Floor";
        sr.sortingOrder = 1;
    }

    // ─────────────────────────────────────────
    // Patrol (simplified from EnemyController)
    // ─────────────────────────────────────────

    private void UpdatePatrol()
    {
        if (patrolPath == null || patrolPath.Length == 0) return;

        PatrolSegment seg = patrolPath[currentSegmentIndex];

        if (seg.duration <= 0f || seg.distance <= 0f)
        {
            AdvanceSegment(segmentStartPosition);
            return;
        }

        segmentTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(segmentTimer / seg.duration);

        Vector2 dir = seg.direction.normalized;
        Vector2 targetPosition = segmentStartPosition + dir * seg.distance;
        rb.MovePosition(Vector2.Lerp(segmentStartPosition, targetPosition, t));

        if (t >= 1f)
            AdvanceSegment(targetPosition);
    }

    private void AdvanceSegment(Vector2 endedAt)
    {
        int nextIndex = currentSegmentIndex + 1;

        if (nextIndex >= patrolPath.Length)
        {
            if (!loopPath) return;
            nextIndex = 0;
        }

        currentSegmentIndex = nextIndex;
        segmentTimer = 0f;
        segmentStartPosition = endedAt;
    }

}
