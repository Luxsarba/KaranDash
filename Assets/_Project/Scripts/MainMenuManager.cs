using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private GameObject menuCamera;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject settings;
    [SerializeField] private GameObject level_selector;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Button continueButton;

    [Header("Save Slots")]
    [SerializeField] private GameObject saveSlotsPanel;
    [SerializeField] private SaveSlotButtonView[] saveSlotViews;
    [SerializeField] private Button saveSlotsBackButton;

    [Header("Intro")]
    [SerializeField] private GameObject introVideoOverlay;
    [SerializeField] private RawImage introVideoImage;
    [SerializeField] private VideoPlayer introVideoPlayer;
    [SerializeField] private Button skipIntroButton;
    [SerializeField] private RenderTexture introRenderTexture;
    [SerializeField] private VideoClip introVideoClip;
    [SerializeField] private string newGameSceneName = "grappletest";

    private Animator _cameraAnimator;
    private bool _introInProgress;
    private bool _menuLocked;
    private int _pendingIntroSlotIndex;
    private RenderTexture _runtimeIntroTexture;

    private void Start()
    {
        if (menuCamera != null)
            _cameraAnimator = menuCamera.GetComponent<Animator>();

        PlayerSessionManager.EnsureSession(playerPrefab);
        HideStandaloneContinue();
        ResolveSceneReferences();
        ConfigureButtons();
        CloseIntroOverlay();
        ShowMainMenu();
        RefreshSaveSlots();
    }

    private void Update()
    {
        if (!_introInProgress)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetMouseButtonDown(0))
        {
            SkipIntro();
        }
    }

    public void StartGame_1() => OpenSaveSlots();
    public void StartGame_2() => OpenSaveSlots();
    public void StartGame_3() => OpenSaveSlots();
    public void StartGame_4() => OpenSaveSlots();

    public void Continue()
    {
        OpenSaveSlots();
    }

    public void Settings()
    {
        if (_menuLocked)
            return;

        SetCameraSecondaryMenu(true);
        if (mainMenu != null)
            mainMenu.SetActive(false);
        if (saveSlotsPanel != null)
            saveSlotsPanel.SetActive(false);
        if (settings != null)
            settings.SetActive(true);
    }

    public void Return_from_settings()
    {
        if (_menuLocked)
            return;

        ShowMainMenu();
    }

    public void Level_selector()
    {
        OpenSaveSlots();
    }

    public void Return_from_level_selector()
    {
        CloseSaveSlots();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void OpenSaveSlots()
    {
        if (_menuLocked)
            return;

        RefreshSaveSlots();
        SetCameraSecondaryMenu(true);
        if (mainMenu != null)
            mainMenu.SetActive(false);
        if (settings != null)
            settings.SetActive(false);
        if (saveSlotsPanel != null)
            saveSlotsPanel.SetActive(true);
    }

    public void CloseSaveSlots()
    {
        if (_menuLocked)
            return;

        ShowMainMenu();
    }

    public void SelectSaveSlot(int slotIndex)
    {
        if (_menuLocked)
            return;

        SaveSlotSummary summary = SaveSystem.GetSlotSummary(slotIndex);
        if (summary != null && summary.hasSave)
        {
            if (summary.isCorrupted)
            {
                Debug.LogWarning($"[MainMenu] Slot {slotIndex} is corrupted and cannot be loaded.");
                return;
            }

            LockMenu();
            PlayerSessionManager.ContinueFromSave(playerPrefab, slotIndex);
            return;
        }

        BeginNewGameFlow(slotIndex);
    }

    public void SkipIntro()
    {
        if (!_introInProgress)
            return;

        FinishIntroAndStartGame();
    }

    private void BeginNewGameFlow(int slotIndex)
    {
        if (string.IsNullOrWhiteSpace(newGameSceneName))
        {
            Debug.LogWarning("[MainMenu] New game scene name is empty.");
            return;
        }

        _pendingIntroSlotIndex = slotIndex;

        if (introVideoClip == null)
        {
            LockMenu();
            PlayerSessionManager.StartNewGame(playerPrefab, newGameSceneName, slotIndex);
            return;
        }

        BeginIntroPlayback();
    }

    private void BeginIntroPlayback()
    {
        ResolveSceneReferences();

        if (introVideoOverlay == null || introVideoPlayer == null || introVideoImage == null)
        {
            Debug.LogWarning("[MainMenu] Intro video overlay is not fully configured. Starting game without intro.");
            LockMenu();
            PlayerSessionManager.StartNewGame(playerPrefab, newGameSceneName, _pendingIntroSlotIndex);
            return;
        }

        LockMenu();
        _introInProgress = true;
        introVideoOverlay.SetActive(true);

        RenderTexture targetTexture = ResolveIntroRenderTexture();
        introVideoPlayer.Stop();
        introVideoPlayer.clip = introVideoClip;
        introVideoPlayer.playOnAwake = false;
        introVideoPlayer.isLooping = false;
        introVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        introVideoPlayer.targetTexture = targetTexture;
        introVideoPlayer.loopPointReached -= HandleIntroFinished;
        introVideoPlayer.loopPointReached += HandleIntroFinished;

        introVideoImage.texture = targetTexture;

        if (skipIntroButton != null)
        {
            skipIntroButton.onClick.RemoveListener(SkipIntro);
            skipIntroButton.onClick.AddListener(SkipIntro);
            skipIntroButton.interactable = true;
        }

        introVideoPlayer.Play();
    }

    private void HandleIntroFinished(VideoPlayer source)
    {
        FinishIntroAndStartGame();
    }

    private void FinishIntroAndStartGame()
    {
        if (introVideoPlayer != null)
        {
            introVideoPlayer.loopPointReached -= HandleIntroFinished;
            introVideoPlayer.Stop();
        }

        CloseIntroOverlay();
        PlayerSessionManager.StartNewGame(playerPrefab, newGameSceneName, _pendingIntroSlotIndex);
    }

    private void ShowMainMenu()
    {
        SetCameraSecondaryMenu(false);

        if (mainMenu != null)
            mainMenu.SetActive(true);
        if (settings != null)
            settings.SetActive(false);
        if (saveSlotsPanel != null)
            saveSlotsPanel.SetActive(false);
    }

    private void RefreshSaveSlots()
    {
        ResolveSceneReferences();

        if (saveSlotViews == null || saveSlotViews.Length == 0)
            return;

        for (int slotIndex = 1; slotIndex <= saveSlotViews.Length; slotIndex++)
        {
            SaveSlotButtonView view = saveSlotViews[slotIndex - 1];
            if (view == null)
                continue;

            SaveSlotSummary summary = SaveSystem.GetSlotSummary(slotIndex);
            bool interactable = summary == null || !summary.isCorrupted;
            int capturedSlot = slotIndex;
            view.Configure(capturedSlot, summary, () => SelectSaveSlot(capturedSlot), interactable && !_menuLocked);
        }

        if (saveSlotsBackButton != null)
            saveSlotsBackButton.interactable = !_menuLocked;
    }

    private void ConfigureButtons()
    {
        if (saveSlotsBackButton != null)
        {
            saveSlotsBackButton.onClick.RemoveListener(CloseSaveSlots);
            saveSlotsBackButton.onClick.AddListener(CloseSaveSlots);
        }

        if (skipIntroButton != null)
        {
            skipIntroButton.onClick.RemoveListener(SkipIntro);
            skipIntroButton.onClick.AddListener(SkipIntro);
        }
    }

    private void LockMenu()
    {
        _menuLocked = true;
        RefreshSaveSlots();
        SetMainMenuInteractable(false);
    }

    private void SetMainMenuInteractable(bool interactable)
    {
        if (mainMenu == null)
            return;

        Button[] buttons = mainMenu.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].interactable = interactable;

        if (saveSlotsBackButton != null)
            saveSlotsBackButton.interactable = interactable;

        if (saveSlotViews == null)
            return;

        for (int i = 0; i < saveSlotViews.Length; i++)
        {
            SaveSlotButtonView view = saveSlotViews[i];
            if (view != null)
                view.SetInteractable(interactable);
        }
    }

    private void HideStandaloneContinue()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    private void CloseIntroOverlay()
    {
        _introInProgress = false;

        if (introVideoOverlay != null)
            introVideoOverlay.SetActive(false);

        if (introVideoImage != null)
            introVideoImage.texture = null;

        if (skipIntroButton != null)
            skipIntroButton.interactable = false;
    }

    private void ResolveSceneReferences()
    {
        if (saveSlotsPanel == null)
        {
            Transform panel = FindInActiveScene("Canvas/SaveSlotsPanel");
            if (panel != null)
                saveSlotsPanel = panel.gameObject;
        }

        if (saveSlotsPanel != null && (saveSlotViews == null || saveSlotViews.Length == 0))
            saveSlotViews = saveSlotsPanel.GetComponentsInChildren<SaveSlotButtonView>(true);

        if (saveSlotsPanel != null && saveSlotsBackButton == null)
        {
            Transform back = saveSlotsPanel.transform.Find("BackButton");
            if (back != null)
                saveSlotsBackButton = back.GetComponent<Button>();
        }

        if (introVideoOverlay == null)
        {
            Transform overlay = FindInActiveScene("Canvas/IntroVideoOverlay");
            if (overlay != null)
                introVideoOverlay = overlay.gameObject;
        }

        if (introVideoOverlay != null && introVideoImage == null)
        {
            Transform videoImage = introVideoOverlay.transform.Find("VideoImage");
            if (videoImage != null)
                introVideoImage = videoImage.GetComponent<RawImage>();
        }

        if (introVideoOverlay != null && introVideoPlayer == null)
            introVideoPlayer = introVideoOverlay.GetComponent<VideoPlayer>();

        if (introVideoOverlay != null && skipIntroButton == null)
        {
            Transform skip = introVideoOverlay.transform.Find("SkipButton");
            if (skip != null)
                skipIntroButton = skip.GetComponent<Button>();
        }
    }

    private RenderTexture ResolveIntroRenderTexture()
    {
        if (introRenderTexture != null)
            return introRenderTexture;

        int width = Mathf.Max(Screen.width, 1280);
        int height = Mathf.Max(Screen.height, 720);
        if (_runtimeIntroTexture != null && _runtimeIntroTexture.width == width && _runtimeIntroTexture.height == height)
            return _runtimeIntroTexture;

        if (_runtimeIntroTexture != null)
            _runtimeIntroTexture.Release();

        _runtimeIntroTexture = new RenderTexture(width, height, 0)
        {
            name = "RuntimeIntroVideo"
        };

        return _runtimeIntroTexture;
    }

    private void SetCameraSecondaryMenu(bool enabled)
    {
        if (_cameraAnimator != null)
            _cameraAnimator.SetBool("IsSettings", enabled);
    }

    private static Transform FindInActiveScene(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string[] parts = path.Split('/');
        if (parts.Length == 0)
            return null;

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null || !string.Equals(root.name, parts[0], StringComparison.Ordinal))
                continue;

            Transform current = root.transform;
            for (int partIndex = 1; partIndex < parts.Length && current != null; partIndex++)
                current = current.Find(parts[partIndex]);

            if (current != null)
                return current;
        }

        return null;
    }

    private void OnDisable()
    {
        _introInProgress = false;
    }

    private void OnDestroy()
    {
        if (introVideoPlayer != null)
            introVideoPlayer.loopPointReached -= HandleIntroFinished;

        if (_runtimeIntroTexture != null)
            _runtimeIntroTexture.Release();
    }
}
