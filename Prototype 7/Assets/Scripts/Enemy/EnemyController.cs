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
/// The enemy's rotation is updated each frame to face the current movement
/// direction so the child flashlight cone naturally points forward.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Patrol Path")]
    [Tooltip("Ordered list of movement segments. Leave empty to keep the enemy stationary.")]
    public PatrolSegment[] patrolPath;

    [Tooltip("If true, the enemy loops back to segment 0 after completing all segments.")]
    public bool loopPath = true;

    private Rigidbody2D rb;
    private int currentSegmentIndex = 0;
    private float segmentTimer = 0f;
    private Vector2 segmentStartPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Start()
    {
        segmentStartPosition = rb.position;
    }

    private void FixedUpdate()
    {
        if (patrolPath == null || patrolPath.Length == 0) return;

        PatrolSegment seg = patrolPath[currentSegmentIndex];

        if (seg.duration <= 0f || seg.distance <= 0f)
        {
            AdvanceSegment();
            return;
        }

        segmentTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(segmentTimer / seg.duration);

        Vector2 dir = seg.direction.normalized;
        Vector2 targetPosition = segmentStartPosition + dir * seg.distance;
        rb.MovePosition(Vector2.Lerp(segmentStartPosition, targetPosition, t));

        RotateToFaceDirection(dir);

        if (t >= 1f)
            AdvanceSegment();
    }

    private void AdvanceSegment()
    {
        int nextIndex = currentSegmentIndex + 1;

        if (nextIndex >= patrolPath.Length)
        {
            if (!loopPath) return;
            nextIndex = 0;
        }

        currentSegmentIndex = nextIndex;
        segmentTimer = 0f;
        segmentStartPosition = rb.position;
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
