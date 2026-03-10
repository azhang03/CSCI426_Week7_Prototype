using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed in world units per second.")]
    public float moveSpeed = 5f;

    [Header("Boundaries")]
    [Tooltip("The Floor tilemap used to clamp the player's position to the map.")]
    public Tilemap floorTilemap;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Vector2 moveInput;
    private bool noclipEnabled = false;

    // World-space boundary values computed once from the tilemap.
    private float minX, maxX, minY, maxY;
    private bool hasBounds = false;

    // Half-size of the player's BoxCollider2D, used to inset the clamp boundary
    // so the player's edge (not its centre) stops at the map edge.
    private Vector2 colliderHalfSize;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        playerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
            colliderHalfSize = col.size * 0.5f;

        ComputeBounds();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
            SetNoclip(!noclipEnabled);
    }

    private void SetNoclip(bool enabled)
    {
        noclipEnabled = enabled;

        // Exclude/restore the player from all collision layers when noclip is toggled.
        if (playerCollider != null)
            playerCollider.excludeLayers = enabled ? ~0 : 0;

        Debug.Log($"[Debug] Noclip {(enabled ? "ON" : "OFF")}");
    }

    private void ComputeBounds()
    {
        if (floorTilemap == null) return;

        Bounds localBounds = floorTilemap.localBounds;
        Vector3 worldMin = floorTilemap.transform.TransformPoint(localBounds.min);
        Vector3 worldMax = floorTilemap.transform.TransformPoint(localBounds.max);

        minX = worldMin.x;
        minY = worldMin.y;
        maxX = worldMax.x;
        maxY = worldMax.y;

        hasBounds = true;
    }

    // Called automatically by the Player Input component when the Move action fires.
    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;

        if (hasBounds && !noclipEnabled)
        {
            Vector2 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minX + colliderHalfSize.x, maxX - colliderHalfSize.x);
            pos.y = Mathf.Clamp(pos.y, minY + colliderHalfSize.y, maxY - colliderHalfSize.y);
            rb.position = pos;
        }
    }
}
