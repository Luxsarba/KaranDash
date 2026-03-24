using UnityEngine;

/// <summary>
/// Триггер для запуска мини-игры Мемори.
/// Навешивается на объект с тегом "MemoryGame".
/// </summary>
public class MemoryGameTrigger : MonoBehaviour
{
    [Header("Ссылка на панель мини-игры")]
    [SerializeField] private MemoryPanel memoryPanel;

    private void Start()
    {
        if (memoryPanel == null)
        {
            memoryPanel = GetComponentInParent<MemoryPanel>();
        }
    }

    public void TriggerGame()
    {
        if (memoryPanel != null)
        {
            memoryPanel.StartGame();
        }
        else
        {
            Debug.LogWarning("MemoryPanel не найден!");
        }
    }
}
