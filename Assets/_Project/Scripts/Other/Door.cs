using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private string targetSpawnPointId;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerSessionManager.TransitionTo(sceneName, targetSpawnPointId);
    }
}
