using UnityEngine;

/// <summary>
/// A serializable data struct defining one segment of an enemy's patrol path.
/// Configure the array via the Inspector on each enemy prefab instance.
/// </summary>
[System.Serializable]
public struct PatrolSegment
{
    [Tooltip("Direction of travel. Will be normalized at runtime. E.g. (1,0) = right, (0,-1) = down.")]
    public Vector2 direction;

    [Tooltip("Distance to travel in world units.")]
    public float distance;

    [Tooltip("Time in seconds to complete this segment.")]
    public float duration;
}

/// <summary>
/// Moves the enemy through a configurable patrol path defined as an array of
/// PatrolSegments. Each segment specifies a direction, distance, and duration.
/// The enemy loops through all segments endlessly (if loopPath is true).
///
/// When "Enable Detection" is toggled on, the enemy also scans the four
/// cardinal directions each frame. If the player is in line-of-sight along
/// any cardinal axis (no Barrier between them), the enemy breaks from its
/// patrol and moves directly toward the player's spotted position. After
/// reaching that position it resumes its patrol loop.
///
/// Detection only activates after the first trigger — before that the enemy
/// follows its patrol path normally.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Patrol Path")]
    [Tooltip("Ordered list of movement segments. Leave empty to keep the enemy stationary.")]
    public PatrolSegment[] patrolPath;

    [Tooltip("If true, the enemy loops back to segment 0 after completing all segments.")]
    public bool loopPath = true;

    [Header("Detection")]
    [Tooltip("Enable cardinal-direction player detection. The enemy will chase the player's spotted position when it has clear LoS.")]
    public bool enableDetection = false;

    [Tooltip("How far the enemy can see along each cardinal direction.")]
    public float detectionRange = 15f;

    [Tooltip("Speed at which the enemy moves toward the player's spotted position.")]
    public float chaseSpeed = 4f;

    [Tooltip("Physics layer mask for Barrier objects that block line-of-sight.")]
    public LayerMask barrierLayerMask;

    private enum State { Patrolling, Chasing }

    private Rigidbody2D rb;
    private Transform playerTransform;

    // Patrol state
    private int currentSegmentIndex = 0;
    private float segmentTimer = 0f;
    private Vector2 segmentStartPosition;

    // Chase state
    private State currentState = State.Patrolling;
    private Vector2 chaseTarget;
    private bool hasBeenTriggered = false;

    private static readonly Vector2[] CardinalDirs = {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        segmentStartPosition = rb.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (enableDetection && !hasBeenTriggered && playerTransform != null)
            TryDetectPlayer();

        switch (currentState)
        {
            case State.Patrolling:
                UpdatePatrol();
                break;
            case State.Chasing:
                UpdateChase();
                break;
        }
    }

    // ─────────────────────────────────────────
    // Detection
    // ─────────────────────────────────────────

    private void TryDetectPlayer()
    {
        Vector2 enemyPos = rb.position;
        Vector2 playerPos = (Vector2)playerTransform.position;

        foreach (Vector2 dir in CardinalDirs)
        {
            if (!IsAlignedOnAxis(enemyPos, playerPos, dir)) continue;

            float dist = Vector2.Distance(enemyPos, playerPos);
            if (dist > detectionRange) continue;

            Vector2 toPlayer = (playerPos - enemyPos).normalized;
            RaycastHit2D hit = Physics2D.Raycast(enemyPos, toPlayer, dist, barrierLayerMask);

            if (hit.collider == null)
            {
                BeginChase(playerPos);
                return;
            }
        }
    }

    /// <summary>
    /// Returns true if the player is roughly aligned with the enemy along
    /// the given cardinal axis (within a small tolerance).
    /// </summary>
    private bool IsAlignedOnAxis(Vector2 enemy, Vector2 player, Vector2 axis)
    {
        const float tolerance = 0.5f;

        if (Mathf.Abs(axis.x) > 0.5f)
        {
            // Horizontal axis — check Y alignment and correct side.
            if (Mathf.Abs(enemy.y - player.y) > tolerance) return false;
            return axis.x > 0 ? player.x > enemy.x : player.x < enemy.x;
        }
        else
        {
            // Vertical axis — check X alignment and correct side.
            if (Mathf.Abs(enemy.x - player.x) > tolerance) return false;
            return axis.y > 0 ? player.y > enemy.y : player.y < enemy.y;
        }
    }

    private void BeginChase(Vector2 target)
    {
        hasBeenTriggered = true;
        currentState = State.Chasing;
        chaseTarget = target;
    }

    // ─────────────────────────────────────────
    // Chase
    // ─────────────────────────────────────────

    private void UpdateChase()
    {
        Vector2 currentPos = rb.position;
        Vector2 toTarget = chaseTarget - currentPos;
        float remaining = toTarget.magnitude;
        float step = chaseSpeed * Time.fixedDeltaTime;

        if (remaining <= step)
        {
            // Snap directly to target — prevents overshooting/oscillation.
            rb.MovePosition(chaseTarget);
            ResumePatrol(chaseTarget);
            return;
        }

        Vector2 dir = toTarget.normalized;
        rb.MovePosition(currentPos + dir * step);
        RotateToFaceDirection(dir);
    }

    private void ResumePatrol(Vector2 exactPosition)
    {
        currentState = State.Patrolling;
        currentSegmentIndex = 0;
        segmentStartPosition = exactPosition;
        segmentTimer = 0f;

        // Face the first patrol segment direction immediately.
        if (patrolPath != null && patrolPath.Length > 0)
            RotateToFaceDirection(patrolPath[0].direction.normalized);
    }

    // ─────────────────────────────────────────
    // Patrol
    // ─────────────────────────────────────────

    private void UpdatePatrol()
    {
        if (!hasBeenTriggered && enableDetection) return;
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

        RotateToFaceDirection(dir);

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

    /// <summary>
    /// Rotates the enemy so that its local "up" axis points in the movement direction.
    /// The flashlight child inherits this rotation, so the cone always faces forward.
    /// </summary>
    private void RotateToFaceDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;
    }
}
