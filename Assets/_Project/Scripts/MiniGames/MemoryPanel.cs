using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Мини-игра "Мемори" — нужно повторить последовательность нажатий.
/// </summary>
public class MemoryPanel : MonoBehaviour
{
    [Header("Настройки игры")]
    [SerializeField, Range(3, 8)] private int sequenceLength = 4;
    [SerializeField, Range(0.5f, 2f)] private float buttonDisplayTime = 1f;
    [SerializeField] private float inputWindowTime = 3f; // Время на ввод одной кнопки

    [Header("Кнопки")]
    [SerializeField] private MemoryButton[] buttons;

    [Header("События")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFail;

    [Header("Аудио (опционально)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip[] buttonClips;

    private List<int> _sequence = new List<int>();
    private int _currentStep = 0;
    private bool _isPlayingSequence;
    private bool _isPlayerTurn;
    private Coroutine _inputTimerCoroutine;

    private void Start()
    {
        if (buttons == null || buttons.Length == 0)
        {
            buttons = GetComponentsInChildren<MemoryButton>(true);
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].Initialize(this, index);
        }

        GenerateSequence();
    }

    private void GenerateSequence()
    {
        _sequence.Clear();
        for (int i = 0; i < sequenceLength; i++)
        {
            _sequence.Add(Random.Range(0, buttons.Length));
        }
    }

    public void StartGame()
    {
        _currentStep = 0;
        _isPlayingSequence = true;
        _isPlayerTurn = false;
        StartCoroutine(PlaySequenceCoroutine());
    }

    private IEnumerator PlaySequenceCoroutine()
    {
        // Пауза перед началом
        yield return new WaitForSeconds(0.5f);

        // Показываем последовательность
        for (int i = 0; i < _sequence.Count; i++)
        {
            yield return new WaitForSeconds(0.3f);
            ActivateButton(_sequence[i]);
        }

        _isPlayingSequence = false;
        _isPlayerTurn = true;
        StartInputTimer();
    }

    private void ActivateButton(int index)
    {
        if (index >= 0 && index < buttons.Length)
        {
            buttons[index].Activate();
            PlayButtonSound(index);
        }
    }

    public void OnButtonPressed(int index)
    {
        if (!_isPlayerTurn || _isPlayingSequence) return;

        PlayButtonSound(index);

        // Проверка правильности нажатия
        if (index == _sequence[_currentStep])
        {
            _currentStep++;

            // Последовательность завершена
            if (_currentStep >= _sequence.Count)
            {
                CompleteGame();
            }
            else
            {
                // Сброс таймера ввода для следующего нажатия
                RestartInputTimer();
            }
        }
        else
        {
            FailGame();
        }
    }

    private void StartInputTimer()
    {
        _inputTimerCoroutine = StartCoroutine(InputTimerCoroutine());
    }

    private void RestartInputTimer()
    {
        if (_inputTimerCoroutine != null)
            StopCoroutine(_inputTimerCoroutine);
        StartInputTimer();
    }

    private IEnumerator InputTimerCoroutine()
    {
        yield return new WaitForSeconds(inputWindowTime);
        if (_isPlayerTurn)
        {
            FailGame();
        }
    }

    private void CompleteGame()
    {
        _isPlayerTurn = false;
        PlaySound(successClip);
        onSuccess?.Invoke();
        enabled = false; // Отключаем скрипт после победы
    }

    private void FailGame()
    {
        _isPlayerTurn = false;
        PlaySound(failClip);
        onFail?.Invoke();

        // Перезапуск через паузу
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        ResetGame();
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        GenerateSequence();
        _currentStep = 0;
        _isPlayingSequence = false;
        _isPlayerTurn = false;
        enabled = true;
    }

    private void PlayButtonSound(int index)
    {
        if (audioSource != null && buttonClips != null && index < buttonClips.Length)
        {
            audioSource.PlayOneShot(buttonClips[index]);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        sequenceLength = Mathf.Clamp(sequenceLength, 3, 8);
        buttonDisplayTime = Mathf.Clamp(buttonDisplayTime, 0.5f, 2f);
    }
#endif
}
