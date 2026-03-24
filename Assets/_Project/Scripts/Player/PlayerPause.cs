using UnityEngine;

/// <summary>
/// Пауза, UI экраны и курсор.
/// </summary>
public class PlayerPause : MonoBehaviour
{
    [Header("UI экраны")]
    [SerializeField] private GameObject playerUI;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    [Header("Настройки")]
    [SerializeField] private bool startPaused = true;

    private bool paused;

    public bool IsPaused => paused;

    private void Start()
    {
        if (startPaused)
        {
            Pause();
            pauseScreen.SetActive(false);
        }
        else
        {
            Continue();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsDialogueOpen())
                return;

            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (paused)
            Continue();
        else
            Pause();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0;
        
        if (playerUI != null)
            playerUI.SetActive(false);
        if (pauseScreen != null)
            pauseScreen.SetActive(true);
        
        GameManager.DisablePlayerInput();
        ShowCursor();
    }

    public void Continue()
    {
        if (IsDialogueOpen())
        {
            paused = true;
            Time.timeScale = 0;

            if (playerUI != null)
                playerUI.SetActive(false);
            if (pauseScreen != null)
                pauseScreen.SetActive(true);

            GameManager.DisablePlayerInput();
            ShowCursor();
            return;
        }

        paused = false;
        Time.timeScale = 1;
        
        if (playerUI != null)
            playerUI.SetActive(true);
        if (pauseScreen != null)
            pauseScreen.SetActive(false);
        
        GameManager.EnablePlayerInput();
        HideCursor();
    }

    public void ShowWinScreen()
    {
        Time.timeScale = 0;
        GameManager.DisablePlayerInput();
        
        if (playerUI != null)
            playerUI.SetActive(false);
        if (winScreen != null)
            winScreen.SetActive(true);
        
        ShowCursor();
    }

    public void ShowLoseScreen()
    {
        Time.timeScale = 0;
        GameManager.DisablePlayerInput();
        
        if (playerUI != null)
            playerUI.SetActive(false);
        if (loseScreen != null)
            loseScreen.SetActive(true);
        
        ShowCursor();
    }

    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public bool GetPaused() => paused;

    private static bool IsDialogueOpen()
    {
        return DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
    }

    /// <summary>
    /// Сброс Time.timeScale при уничтожении (защита от утечки).
    /// </summary>
    private void OnDestroy()
    {
        Time.timeScale = 1;
    }
}
