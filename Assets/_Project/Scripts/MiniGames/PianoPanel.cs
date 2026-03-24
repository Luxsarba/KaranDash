using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Piano puzzle panel: keys are always playable and a configured melody triggers success.
/// </summary>
public class PianoPanel : MonoBehaviour
{
    [Header("Melody")]
    [Tooltip("Expected sequence, e.g. C4 D#4 A5 Bb4")]
    [SerializeField] private string melodySequence = "C4 D4 E4";

    [Header("References")]
    [SerializeField] private PianoKey[] keys;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;

    [Header("Success Audio")]
    [SerializeField] private AudioSource successAudioSource;
    [SerializeField] private AudioClip successMelodyClip;
    [SerializeField, Range(0f, 1f)] private float successMelodyVolume = 1f;

    private readonly List<string> _expectedNotes = new List<string>();
    private int _progressIndex;
    private bool _hasValidMelody;

    private void Awake()
    {
        ResolveSuccessAudioSource();
        InitializeKeys();
        RebuildMelodyCache();
    }

    private void OnEnable()
    {
        ResolveSuccessAudioSource();
        InitializeKeys();
        RebuildMelodyCache();
    }

    public void StartGame()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        _progressIndex = 0;
        ApplyKeyMarks();
    }

    public bool TryInteractWithKey(PianoKey key)
    {
        if (key == null)
            return false;

        if (!_hasValidMelody || _expectedNotes.Count == 0)
            return true;

        string normalized = key.NormalizedNoteId;
        if (string.IsNullOrEmpty(normalized))
        {
            ResetProgressWithFail();
            return true;
        }

        if (normalized == _expectedNotes[_progressIndex])
        {
            _progressIndex++;

            if (_progressIndex >= _expectedNotes.Count)
            {
                _progressIndex = 0;
                PlaySuccessMelody();
                onSuccess?.Invoke();
            }

            return true;
        }

        ResetProgressWithFail();
        return true;
    }

    private void ResetProgressWithFail()
    {
        _progressIndex = 0;
    }

    private void InitializeKeys()
    {
        keys = GetComponentsInChildren<PianoKey>(true);

        if (keys == null)
            return;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] != null)
                keys[i].Initialize(this);
        }

        ApplyKeyMarks();
    }

    private void ApplyKeyMarks()
    {
        if (keys == null)
            return;

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (key == null)
                continue;

            key.SetMarked(false);
        }
    }

    private void RebuildMelodyCache()
    {
        _expectedNotes.Clear();
        _progressIndex = 0;

        _hasValidMelody = TryParseMelody(melodySequence, _expectedNotes);
        if (!_hasValidMelody)
        {
            Debug.LogWarning($"[PianoPanel] Melody has invalid note format on '{name}'. Use format like C4 D#4 A5.", this);
            ApplyKeyMarks();
            return;
        }

        ApplyKeyMarks();
    }

    private void PlaySuccessMelody()
    {
        if (successMelodyClip == null)
            return;

        ResolveSuccessAudioSource();
        if (successAudioSource != null)
            successAudioSource.PlayOneShot(successMelodyClip, successMelodyVolume);
    }

    private void ResolveSuccessAudioSource()
    {
        if (successAudioSource == null)
            successAudioSource = GetComponent<AudioSource>();

        if (successAudioSource == null && Application.isPlaying)
        {
            successAudioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSuccessAudioSource();
    }

    private void ConfigureSuccessAudioSource()
    {
        if (successAudioSource == null)
            return;

        successAudioSource.playOnAwake = false;
        successAudioSource.spatialBlend = 0f;
        successAudioSource.dopplerLevel = 0f;
        successAudioSource.minDistance = 2f;
        successAudioSource.maxDistance = 20f;
    }

    private static bool TryParseMelody(string input, List<string> buffer)
    {
        buffer.Clear();

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] tokens = input.Split(new[] { ' ', '\t', '\r', '\n', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return false;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!TryNormalizeNoteToken(tokens[i], out string normalized))
                return false;

            buffer.Add(normalized);
        }

        return buffer.Count > 0;
    }

    public static bool TryNormalizeNoteToken(string token, out string normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        string value = token.Trim().ToUpperInvariant();
        if (value.Length < 2)
            return false;

        char letter = value[0];
        int baseSemitone;
        switch (letter)
        {
            case 'C': baseSemitone = 0; break;
            case 'D': baseSemitone = 2; break;
            case 'E': baseSemitone = 4; break;
            case 'F': baseSemitone = 5; break;
            case 'G': baseSemitone = 7; break;
            case 'A': baseSemitone = 9; break;
            case 'B': baseSemitone = 11; break;
            default: return false;
        }

        int index = 1;
        int accidental = 0;
        if (index < value.Length && (value[index] == '#' || value[index] == 'B'))
        {
            accidental = value[index] == '#' ? 1 : -1;
            index++;
        }

        if (index >= value.Length)
            return false;

        if (!int.TryParse(value.Substring(index), out int octave))
            return false;

        int semitone = baseSemitone + accidental;
        while (semitone < 0)
        {
            semitone += 12;
            octave--;
        }

        while (semitone >= 12)
        {
            semitone -= 12;
            octave++;
        }

        string[] sharpNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        normalized = sharpNames[semitone] + octave;
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        InitializeKeys();
        RebuildMelodyCache();
    }
#endif
}
