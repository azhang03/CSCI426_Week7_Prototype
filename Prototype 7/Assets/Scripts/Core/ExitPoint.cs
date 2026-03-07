using UnityEngine;

/// <summary>
/// Place on a trigger collider at the level exit.
/// When the Player enters, notifies the GameManager that the level is complete.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitPoint : MonoBehaviour
{
    private void Awake()
    {
        // Ensure the collider is always a trigger.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance.OnLevelComplete();
    }
}
