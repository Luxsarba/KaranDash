using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadOnStart : MonoBehaviour
{
    [SerializeField] private bool loadAutomatically = true;

    private void Start()
    {
        if (!loadAutomatically)
        {
            CollectedWorldObjectState.Clear();
            QuestProgressState.Clear();
            Debug.LogWarning("[SaveSystem] auto load disabled");
            return;
        }

        SaveData data = SaveSystem.Load();
        if (data == null)
        {
            CollectedWorldObjectState.Clear();
            QuestProgressState.Clear();
            Debug.LogWarning("[SaveSystem] no data");
            return;
        }

        Player player = GameManager.player;
        if (player == null)
        {
            Debug.LogWarning("[SaveSystem] no player");
            return;
        }

        PlayerInventory inv = player.GetComponent<PlayerInventory>();
        SaveSystem.ApplyToPlayer(player, inv, data);

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[LoadOnStart] Applied save in scene '{currentScene}' from '{data.sceneName}'");
    }
}
