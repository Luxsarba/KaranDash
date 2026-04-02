using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoCutsceneOverlayPlayer : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Button skipButton;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private bool returnToMenuOnFinish = true;

    private bool _isPlaying;
    private RenderTexture _runtimeTexture;

    private void Awake()
    {
        ResolveReferences();
        HideOverlay();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetMouseButtonDown(0))
        {
            FinishPlayback();
        }
    }

    public void Play()
    {
        ResolveReferences();

        if (_isPlaying)
            return;

        if (videoClip == null)
        {
            Debug.LogWarning("[VideoCutsceneOverlayPlayer] VideoClip is not assigned.", this);
            return;
        }

        if (overlayRoot == null || videoPlayer == null || videoImage == null)
        {
            Debug.LogWarning("[VideoCutsceneOverlayPlayer] Overlay references are incomplete.", this);
            return;
        }

        _isPlaying = true;

        RenderTexture targetTexture = ResolveRenderTexture();
        videoPlayer.Stop();
        videoPlayer.clip = videoClip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = targetTexture;
        videoPlayer.loopPointReached -= HandleLoopPointReached;
        videoPlayer.loopPointReached += HandleLoopPointReached;

        videoImage.texture = targetTexture;

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(FinishPlayback);
            skipButton.onClick.AddListener(FinishPlayback);
            skipButton.interactable = true;
        }

        OverlayModalController.Show(overlayRoot);
        videoPlayer.Play();
    }

    private void HandleLoopPointReached(VideoPlayer source)
    {
        FinishPlayback();
    }

    private void FinishPlayback()
    {
        if (!_isPlaying)
            return;

        _isPlaying = false;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleLoopPointReached;
            videoPlayer.Stop();
        }

        HideOverlay();

        if (returnToMenuOnFinish)
            PlayerSessionManager.ReturnToMenu();
    }

    private void HideOverlay()
    {
        if (skipButton != null)
            skipButton.interactable = false;

        if (videoImage != null)
            videoImage.texture = null;

        if (overlayRoot != null && overlayRoot.activeSelf)
            OverlayModalController.Hide(overlayRoot);
        else if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (overlayRoot == null)
        {
            Transform overlay = transform.Find("Overlay");
            if (overlay != null)
                overlayRoot = overlay.gameObject;
        }

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (overlayRoot != null && videoImage == null)
        {
            Transform image = overlayRoot.transform.Find("VideoImage");
            if (image != null)
                videoImage = image.GetComponent<RawImage>();
        }

        if (overlayRoot != null && skipButton == null)
        {
            Transform skip = overlayRoot.transform.Find("SkipButton");
            if (skip != null)
                skipButton = skip.GetComponent<Button>();
        }
    }

    private RenderTexture ResolveRenderTexture()
    {
        if (renderTexture != null)
            return renderTexture;

        int width = Mathf.Max(Screen.width, 1280);
        int height = Mathf.Max(Screen.height, 720);
        if (_runtimeTexture != null && _runtimeTexture.width == width && _runtimeTexture.height == height)
            return _runtimeTexture;

        if (_runtimeTexture != null)
            _runtimeTexture.Release();

        _runtimeTexture = new RenderTexture(width, height, 0)
        {
            name = "RuntimeCutsceneVideo"
        };

        return _runtimeTexture;
    }

    private void OnDisable()
    {
        _isPlaying = false;

        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleLoopPointReached;
    }

    private void OnDestroy()
    {
        if (_runtimeTexture != null)
            _runtimeTexture.Release();
    }
}
