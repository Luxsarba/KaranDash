using System.Collections.Generic;
using UnityEngine;

public class SlowZone : MonoBehaviour
{
    private struct PlayerSlowState
    {
        public float OriginalSpeedMultiplier;
        public int ContactsInsideZone;
    }

    [Header("Slow settings")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slowMultiplier = 0.4f;

    [SerializeField] private string playerTag = "Player";

    private readonly Dictionary<Player, PlayerSlowState> _playersInZone = new Dictionary<Player, PlayerSlowState>();

    private void OnTriggerEnter(Collider other)
    {
        if (!TryResolvePlayer(other, out var player))
            return;

        if (_playersInZone.TryGetValue(player, out var state))
        {
            state.ContactsInsideZone++;
            _playersInZone[player] = state;
            return;
        }

        _playersInZone[player] = new PlayerSlowState
        {
            OriginalSpeedMultiplier = player.speedMultiplier,
            ContactsInsideZone = 1
        };

        player.speedMultiplier = slowMultiplier;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryResolvePlayer(other, out var player))
            return;

        if (!_playersInZone.TryGetValue(player, out var state))
            return;

        state.ContactsInsideZone--;

        if (state.ContactsInsideZone <= 0)
        {
            player.speedMultiplier = state.OriginalSpeedMultiplier;
            _playersInZone.Remove(player);
        }
        else
        {
            _playersInZone[player] = state;
        }
    }

    private void OnDisable()
    {
        RestorePlayers();
    }

    private void OnDestroy()
    {
        RestorePlayers();
    }

    private bool TryResolvePlayer(Collider other, out Player player)
    {
        player = other != null ? other.GetComponentInParent<Player>() : null;
        if (player == null)
            return false;

        if (!string.IsNullOrEmpty(playerTag) && !player.CompareTag(playerTag))
            return false;

        return true;
    }

    private void RestorePlayers()
    {
        foreach (var pair in _playersInZone)
        {
            if (pair.Key != null)
                pair.Key.speedMultiplier = pair.Value.OriginalSpeedMultiplier;
        }

        _playersInZone.Clear();
    }
}
