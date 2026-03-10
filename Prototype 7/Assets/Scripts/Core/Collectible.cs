using System;
using UnityEngine;

/// <summary>
/// A pickup item that bobs up and down and is collected on contact with
/// the Player. Fires a static event with its ID so other systems (like
/// gated LightZones) can react without a direct reference.
///
/// Prefab: SpriteRenderer, CircleCollider2D (trigger), Rigidbody2D (Kinematic),
/// this script. Place on the Entities sorting layer.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Collectible : MonoBehaviour
{
    /// <summary>Fires when any collectible is picked up. Payload is the collectibleID.</summary>
    public static event Action<string> OnCollected;

    [Header("Identity")]
    [Tooltip("Unique ID used to link this collectible to a gated LightZone. Leave empty for plain pickups.")]
    public string collectibleID = "";

    [Header("Audio")]
    [Tooltip("Sound played when this collectible is picked up.")]
    public AudioClip collectSound;

    [Range(0f, 1f)]
    [Tooltip("Volume of the collect sound.")]
    public float collectVolume = 1f;

    [Header("Bob Animation")]
    [Tooltip("How far up and down the item floats (world units).")]
    public float bobHeight = 0.15f;

    [Tooltip("Speed of the bobbing oscillation.")]
    public float bobSpeed = 2f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.position;

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        Vector3 pos = basePosition;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);

        OnCollected?.Invoke(collectibleID);
        Destroy(gameObject);
    }
}
