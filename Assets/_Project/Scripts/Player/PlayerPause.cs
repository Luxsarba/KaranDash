using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private bool startPaused = false;

    private bool paused;
    private bool endScreenLocked;

    public bool IsPaused => paused;

    private void Start()
    {
        ResolveUiReferences();
        WireButtons();

        if (startPaused)
        {
            Pause();
            if (pauseScreen != null)
                pauseScreen.SetActive(false);
        }
        else
        {
            Continue();
        }
    }

    private void Update()
    {
        if (endScreenLocked)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsOverlayOpen())
                return;

            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (endScreenLocked)
            return;

        if (paused)
            Continue();
        else
            Pause();
    }

    public void Pause()
    {
        if (endScreenLocked)
            return;

        paused = true;
        Time.timeScale = 0;

        SetGameplayUiVisible(false);
        if (pauseScreen != null)
            pauseScreen.SetActive(true);
        
        GameManager.DisablePlayerInput();
        ShowCursor();
    }

    public void Continue()
    {
        if (endScreenLocked)
            return;

        if (IsOverlayOpen())
        {
            paused = true;
            Time.timeScale = 0;

            SetGameplayUiVisible(false);
            if (pauseScreen != null)
                pauseScreen.SetActive(true);

            GameManager.DisablePlayerInput();
            ShowCursor();
            return;
        }

        paused = false;
        Time.timeScale = 1;

        SetGameplayUiVisible(true);
        if (pauseScreen != null)
            pauseScreen.SetActive(false);
        
        GameManager.EnablePlayerInput();
        HideCursor();
    }

    public void ShowWinScreen()
    {
        endScreenLocked = true;
        paused = true;
        Time.timeScale = 0;
        GameManager.DisablePlayerInput();

        SetGameplayUiVisible(false);
        if (winScreen != null)
            winScreen.SetActive(true);
        
        ShowCursor();
    }

    public void ShowLoseScreen()
    {
        endScreenLocked = true;
        paused = true;
        Time.timeScale = 0;
        GameManager.DisablePlayerInput();

        SetGameplayUiVisible(false);
        if (loseScreen != null)
            loseScreen.SetActive(true);
        
        ShowCursor();
    }

    public void Restart()
    {
        endScreenLocked = false;
        paused = false;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        endScreenLocked = false;
        paused = false;
        Time.timeScale = 1;
        PlayerSessionManager.ReturnToMenu();
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

    private static bool IsOverlayOpen()
    {
        return OverlayModalController.HasOpenOverlay;
    }

    private void SetGameplayUiVisible(bool visible)
    {
        if (playerUI == null)
            return;

        // If modal screens are children of playerUI, disabling the whole root hides them too.
        if (ContainsScreen(playerUI, pauseScreen) ||
            ContainsScreen(playerUI, winScreen) ||
            ContainsScreen(playerUI, loseScreen))
        {
            return;
        }

        playerUI.SetActive(visible);
    }

    private static bool ContainsScreen(GameObject root, GameObject screen)
    {
        if (root == null || screen == null)
            return false;

        return screen.transform.IsChildOf(root.transform);
    }

    /// <summary>
    /// Сброс Time.timeScale при уничтожении (защита от утечки).
    /// </summary>
    private void OnDestroy()
    {
        Time.timeScale = 1;
    }

    private void ResolveUiReferences()
    {
        if (playerUI == null)
            playerUI = FindByName("PlayerUI");

        if (pauseScreen == null)
            pauseScreen = FindByName("PauseScreen");

        if (winScreen == null)
            winScreen = FindByName("WinScreen");

        if (loseScreen == null)
            loseScreen = FindByName("LoseScreen");
    }

    private void WireButtons()
    {
        BindButton(pauseScreen, "Continue", Continue);
        BindButton(pauseScreen, "Restart", Restart);
        BindButton(pauseScreen, "Quit", Quit);

        BindButton(winScreen, "Restart", Restart);
        BindButton(winScreen, "Quit", Quit);

        BindButton(loseScreen, "Restart", Restart);
        BindButton(loseScreen, "Quit", Quit);
    }

    private static void BindButton(GameObject root, string nameHint, UnityEngine.Events.UnityAction callback)
    {
        if (root == null || callback == null)
            return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            string lowerName = button.name.ToLowerInvariant();
            if (!lowerName.Contains(nameHint.ToLowerInvariant()))
                continue;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }
    }

    private static GameObject FindByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;

            if (!string.Equals(t.name, targetName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (!t.gameObject.scene.IsValid())
                continue;

            return t.gameObject;
        }

        return null;
    }
}
