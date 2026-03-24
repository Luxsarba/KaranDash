using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Memory mini-game: watch the sequence and replay it.
/// </summary>
public class MemoryPanel : MonoBehaviour
{
    public static MemoryPanel ActiveGamePanel { get; private set; }

    [Header("Game")]
    [SerializeField, Range(3, 12)] private int sequenceLength = 6;
    [SerializeField, Range(1, 8)] private int startRoundLength = 2;
    [SerializeField, Min(0.05f)] private float initialShowDelay = 0.5f;
    [SerializeField, Min(0.05f)] private float buttonDisplayTime = 0.6f;
    [SerializeField, Min(0f)] private float pauseBetweenButtons = 0.25f;
    [SerializeField, Min(0.2f)] private float inputWindowTime = 3f;
    [SerializeField] private bool autoRestartOnFail = true;
    [SerializeField, Min(0f)] private float restartDelay = 1.5f;
    [Header("Buttons")]
    [SerializeField] private MemoryButton[] buttons;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFail;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip[] buttonClips;

    private readonly List<int> _sequence = new List<int>();
    private Coroutine _inputTimerCoroutine;
    private int _currentRoundLength;
    private int _currentInputIndex;
    private bool _isShowingSequence;
    private bool _isPlayerTurn;
    private bool _isGameRunning;

    public static bool IsAnyGameRunning()
    {
        return ActiveGamePanel != null && ActiveGamePanel._isGameRunning;
    }

    public static bool IsInteractionInputAllowed()
    {
        return ActiveGamePanel != null &&
               ActiveGamePanel._isGameRunning &&
               ActiveGamePanel._isPlayerTurn &&
               !ActiveGamePanel._isShowingSequence;
    }

    private void Awake()
    {
        InitializeButtons();
    }

    private void OnEnable()
    {
        InitializeButtons();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopInputTimer();
        _isGameRunning = false;
        _isShowingSequence = false;
        _isPlayerTurn = false;

        if (ActiveGamePanel == this)
            ActiveGamePanel = null;
    }

    public void StartGame()
    {
        if (!EnsureButtonsReady())
        {
            Debug.LogWarning("[MemoryPanel] No buttons configured.", this);
            return;
        }

        StopAllCoroutines();
        StopInputTimer();
        ResetButtonsToNormal();

        _isGameRunning = true;
        _isShowingSequence = false;
        _isPlayerTurn = false;
        _currentInputIndex = 0;
        ActiveGamePanel = this;

        GenerateSequence();
        _currentRoundLength = Mathf.Clamp(startRoundLength, 1, _sequence.Count);
        StartCoroutine(PlayRoundCoroutine());
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        StopInputTimer();
        _isGameRunning = false;
        _isShowingSequence = false;
        _isPlayerTurn = false;
        _currentInputIndex = 0;
        _currentRoundLength = 0;
        
        if (ActiveGamePanel == this)
            ActiveGamePanel = null;

        GenerateSequence();
        ResetButtonsToNormal();
    }

    public bool TryInteractWithButton(MemoryButton button)
    {
        if (button == null || !button.IsOwnedBy(this))
            return false;

        if (!_isGameRunning || !_isPlayerTurn || _isShowingSequence)
            return false;

        OnButtonPressed(button.ButtonIndex);
        return true;
    }

    public void OnButtonPressed(int index)
    {
        if (!_isGameRunning || !_isPlayerTurn || _isShowingSequence)
            return;

        if (index < 0 || index >= buttons.Length)
            return;

        ActivateButton(index, buttonDisplayTime * 0.6f);

        if (index != _sequence[_currentInputIndex])
        {
            buttons[index].SetFail();
            FailGame();
            return;
        }

        _currentInputIndex++;

        if (_currentInputIndex >= _currentRoundLength)
        {
            StopInputTimer();

            if (_currentRoundLength >= _sequence.Count)
            {
                CompleteGame();
                return;
            }

            _currentRoundLength++;
            _isPlayerTurn = false;
            StartCoroutine(NextRoundDelayCoroutine());
            return;
        }

        RestartInputTimer();
    }

    private IEnumerator NextRoundDelayCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        yield return PlayRoundCoroutine();
    }

    private IEnumerator PlayRoundCoroutine()
    {
        _isShowingSequence = true;
        _isPlayerTurn = false;
        _currentInputIndex = 0;
        StopInputTimer();

        yield return new WaitForSecondsRealtime(initialShowDelay);

        for (int i = 0; i < _currentRoundLength; i++)
        {
            ActivateButton(_sequence[i], buttonDisplayTime);
            yield return new WaitForSecondsRealtime(buttonDisplayTime + pauseBetweenButtons);
        }

        _isShowingSequence = false;
        _isPlayerTurn = true;
        StartInputTimer();
    }

    private void CompleteGame()
    {
        if (!_isGameRunning)
            return;

        _isGameRunning = false;
        _isShowingSequence = false;
        _isPlayerTurn = false;
        StopInputTimer();

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetSuccess();

        PlaySound(successClip);
        onSuccess?.Invoke();
        StartCoroutine(FinishSuccessCoroutine());
    }

    private IEnumerator FinishSuccessCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        ResetButtonsToNormal();

        if (ActiveGamePanel == this)
            ActiveGamePanel = null;
    }

    private void FailGame()
    {
        if (!_isGameRunning)
            return;

        _isGameRunning = false;
        _isShowingSequence = false;
        _isPlayerTurn = false;
        StopInputTimer();

        PlaySound(failClip);
        onFail?.Invoke();

        if (autoRestartOnFail)
            StartCoroutine(RestartAfterDelayCoroutine());
        else
        {
            if (ActiveGamePanel == this)
                ActiveGamePanel = null;
        }
    }

    private IEnumerator RestartAfterDelayCoroutine()
    {
        yield return new WaitForSecondsRealtime(restartDelay);
        StartGame();
    }

    private void StartInputTimer()
    {
        StopInputTimer();
        _inputTimerCoroutine = StartCoroutine(InputTimerCoroutine());
    }

    private void RestartInputTimer()
    {
        StartInputTimer();
    }

    private void StopInputTimer()
    {
        if (_inputTimerCoroutine != null)
        {
            StopCoroutine(_inputTimerCoroutine);
            _inputTimerCoroutine = null;
        }
    }

    private IEnumerator InputTimerCoroutine()
    {
        yield return new WaitForSecondsRealtime(inputWindowTime);

        if (_isGameRunning && _isPlayerTurn)
            FailGame();
    }

    private void ActivateButton(int index, float activeTime)
    {
        if (index < 0 || index >= buttons.Length)
            return;

        buttons[index].Activate(activeTime);
        PlayButtonSound(index);
    }

    private bool EnsureButtonsReady()
    {
        if (buttons == null || buttons.Length == 0)
            buttons = GetComponentsInChildren<MemoryButton>(true);

        return buttons != null && buttons.Length > 0;
    }

    private void InitializeButtons()
    {
        if (!EnsureButtonsReady())
            return;

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].Initialize(this, i);
    }

    private void GenerateSequence()
    {
        if (!EnsureButtonsReady())
            return;

        _sequence.Clear();
        int targetLength = Mathf.Max(1, sequenceLength);

        for (int i = 0; i < targetLength; i++)
            _sequence.Add(Random.Range(0, buttons.Length));
    }

    private void ResetButtonsToNormal()
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].SetNormal();
        }
    }

    private void PlayButtonSound(int index)
    {
        if (audioSource == null || buttonClips == null || index >= buttonClips.Length)
            return;

        var clip = buttonClips[index];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        sequenceLength = Mathf.Clamp(sequenceLength, 3, 12);
        startRoundLength = Mathf.Clamp(startRoundLength, 1, sequenceLength);
        initialShowDelay = Mathf.Max(0.05f, initialShowDelay);
        buttonDisplayTime = Mathf.Max(0.05f, buttonDisplayTime);
        pauseBetweenButtons = Mathf.Max(0f, pauseBetweenButtons);
        inputWindowTime = Mathf.Max(0.2f, inputWindowTime);
        restartDelay = Mathf.Max(0f, restartDelay);
    }
#endif
}
