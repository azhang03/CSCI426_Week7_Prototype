using UnityEngine;

/// <summary>
/// An invisible checkpoint that activates when a specific Collectible is
/// picked up. Once active, the player respawns here on death instead of
/// at the level start.
///
/// Place an empty GameObject with this script at the desired respawn
/// position. Set requiredCollectibleID to match a Collectible in the scene.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Tooltip("The collectible ID that activates this checkpoint. Must match a Collectible's collectibleID.")]
    public string requiredCollectibleID = "";

    private void OnEnable()
    {
        Collectible.OnCollected += HandleCollectibleCollected;
    }

    private void OnDisable()
    {
        Collectible.OnCollected -= HandleCollectibleCollected;
    }

    private void HandleCollectibleCollected(string id)
    {
        if (string.IsNullOrEmpty(requiredCollectibleID)) return;
        if (id != requiredCollectibleID) return;

        if (GameManager.Instance != null)
            GameManager.Instance.SetCheckpoint(transform.position);
    }
}
