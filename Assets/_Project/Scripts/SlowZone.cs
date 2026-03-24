using UnityEngine;

public class SlowZone : MonoBehaviour
{
    [Header("Настройки замедления")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slowMultiplier = 0.4f;

    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var player = other.GetComponentInParent<Player>();
        if (player)
        {
            player.speedMultiplier = slowMultiplier;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var player = other.GetComponentInParent<Player>();
        if (player)
        {
            player.speedMultiplier = 1f;
        }
    }
}
