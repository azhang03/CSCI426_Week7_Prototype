using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a "melt into red pixels" death animation on the player.
/// Attach to the Player GameObject and assign Square.png as the pixelSprite.
///
/// When Play() is called, the player's sprite is hidden and small red pixel
/// GameObjects scatter downward from the sprite's area, fading out over time.
/// After the animation completes, the onComplete callback is invoked so the
/// caller (GameManager) can proceed with respawn.
/// </summary>
public class DeathEffect : MonoBehaviour
{
    [Header("Pixel Settings")]
    [Tooltip("Sprite used for each pixel particle. Assign Square.png.")]
    public Sprite pixelSprite;

    [Tooltip("Number of pixel particles to spawn.")]
    public int pixelCount = 30;

    [Tooltip("Duration of the melt animation in seconds.")]
    public float animationDuration = 1f;

    [Tooltip("Size of each pixel particle in world units.")]
    public float pixelSize = 0.06f;

    public void Play(Action onComplete)
    {
        StartCoroutine(MeltRoutine(onComplete));
    }

    private IEnumerator MeltRoutine(Action onComplete)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Bounds bounds = sr != null ? sr.bounds : new Bounds(transform.position, Vector3.one * 0.5f);

        List<Transform> pixels = new List<Transform>(pixelCount);
        List<SpriteRenderer> pixelRenderers = new List<SpriteRenderer>(pixelCount);
        List<Vector2> velocities = new List<Vector2>(pixelCount);

        for (int i = 0; i < pixelCount; i++)
        {
            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            GameObject px = new GameObject("DeathPixel");
            px.transform.position = new Vector3(x, y, transform.position.z - 0.01f);
            px.transform.localScale = Vector3.one * pixelSize;

            SpriteRenderer pxSr = px.AddComponent<SpriteRenderer>();
            pxSr.sprite = pixelSprite;
            float r = UnityEngine.Random.Range(0.6f, 1f);
            float g = UnityEngine.Random.Range(0f, 0.1f);
            float b = UnityEngine.Random.Range(0f, 0.05f);
            pxSr.color = new Color(r, g, b, 1f);
            pxSr.sortingLayerName = "Entities";
            pxSr.sortingOrder = 100;

            float vx = UnityEngine.Random.Range(-0.5f, 0.5f);
            float vy = UnityEngine.Random.Range(-1.5f, -0.3f);

            pixels.Add(px.transform);
            pixelRenderers.Add(pxSr);
            velocities.Add(new Vector2(vx, vy));
        }

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            for (int i = 0; i < pixels.Count; i++)
            {
                if (pixels[i] == null) continue;

                pixels[i].position += (Vector3)(velocities[i] * Time.deltaTime);

                Color c = pixelRenderers[i].color;
                c.a = 1f - t;
                pixelRenderers[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < pixels.Count; i++)
        {
            if (pixels[i] != null)
                Destroy(pixels[i].gameObject);
        }

        onComplete?.Invoke();
    }
}
