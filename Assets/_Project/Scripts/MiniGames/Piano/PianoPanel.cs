using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// Piano puzzle panel: keys are always playable and a configured melody triggers success.
/// </summary>
public class PianoPanel : MonoBehaviour
{
    private static readonly char[] MelodySeparators = { ' ', '\t', '\r', '\n', ',', ';' };

    [Header("Melody")]
    [Tooltip("Expected sequence, e.g. C4 D#4 A5 Bb4")]
    [SerializeField] private string melodySequence = "C4 D4 E4";

    [Header("References")]
    [SerializeField] private PianoKey[] keys;

    [Header("Events")]
    [SerializeField] private UnityEvent onSuccess;

    private readonly List<string> _expectedNotes = new List<string>();
    private int _progressIndex;
    private bool _hasValidMelody;
    public event Action Success;

    private void Awake()
    {
        InitializePanel();
    }

    private void OnEnable()
    {
        InitializePanel();
    }

    public void StartGame()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        _progressIndex = 0;
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
                HandleSuccess();
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

    private void InitializePanel()
    {
        InitializeKeys();
        RebuildMelodyCache();
    }

    private void InitializeKeys()
    {
        if (keys == null || keys.Length == 0)
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
            return;
        }
    }

    private void HandleSuccess()
    {
        Success?.Invoke();
        onSuccess?.Invoke();
    }

    private static bool TryParseMelody(string input, List<string> buffer)
    {
        buffer.Clear();

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string[] tokens = input.Split(MelodySeparators, System.StringSplitOptions.RemoveEmptyEntries);
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
