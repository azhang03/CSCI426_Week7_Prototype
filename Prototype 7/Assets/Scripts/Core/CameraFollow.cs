using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Attaches to the Main Camera. Follows the player each LateUpdate, then
/// clamps the camera position so the viewport never shows outside the
/// floor tilemap boundaries.
///
/// If the map is smaller than the camera viewport in either axis the camera
/// simply centres on the map in that axis.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player Transform to follow.")]
    public Transform target;

    [Tooltip("The Floor tilemap used to determine world-space boundaries.")]
    public Tilemap floorTilemap;

    // World-space bounds computed once from the tilemap.
    private float minX, maxX, minY, maxY;

    // Camera half-extents computed once.
    private float camHalfHeight, camHalfWidth;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        ComputeBounds();
    }

    private void ComputeBounds()
    {
        if (floorTilemap == null) return;

        // cellBounds is in local cell coordinates; convert to world space.
        Bounds localBounds = floorTilemap.localBounds;
        Vector3 worldMin = floorTilemap.transform.TransformPoint(localBounds.min);
        Vector3 worldMax = floorTilemap.transform.TransformPoint(localBounds.max);

        minX = worldMin.x;
        minY = worldMin.y;
        maxX = worldMax.x;
        maxY = worldMax.y;

        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Recompute half-extents every frame in case the screen is resized.
        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;

        float x = target.position.x;
        float y = target.position.y;

        // Clamp so the viewport edge never exceeds the tilemap edge.
        // If map is smaller than the viewport, centre on the map midpoint.
        float clampedX = (maxX - minX) < camHalfWidth * 2f
            ? (minX + maxX) * 0.5f
            : Mathf.Clamp(x, minX + camHalfWidth, maxX - camHalfWidth);

        float clampedY = (maxY - minY) < camHalfHeight * 2f
            ? (minY + maxY) * 0.5f
            : Mathf.Clamp(y, minY + camHalfHeight, maxY - camHalfHeight);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
