using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Piano key that can be pressed through player interaction.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PianoKey : MonoBehaviour, IPlayerInteractable
{
    [Header("Note")]
    [Tooltip("Format: C4, D#4, Bb4, A5")]
    [SerializeField] private string noteId = "C4";

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.6f, 1f, 0.8f);
    [SerializeField, Min(0.03f)] private float flashDuration = 0.18f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip noteClip;
    [SerializeField, Range(0.05f, 1f)] private float generatedToneDuration = 0.22f;
    [SerializeField, Range(0f, 1f)] private float generatedToneVolume = 0.2f;

    [Header("Optional")]
    [SerializeField] private bool allowMouseClick;

    private PianoPanel _panel;
    private string _normalizedNoteId;
    private const int GeneratedToneSampleRate = 44100;
    private static readonly Dictionary<string, AudioClip> GeneratedToneCache = new Dictionary<string, AudioClip>();
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _propertyBlock;

    public string NormalizedNoteId => _normalizedNoteId;
    public bool TryInteract(PlayerInteractionContext context)
    {
        return TryPressFromInteraction();
    }


    private void Awake()
    {
        EnsureRenderer();

        EnsureAudioSource();

        NormalizeNote();
        ApplyIdleColor();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ResetAfterFlash));
    }

    public void Initialize(PianoPanel panel)
    {
        _panel = panel;
        if (_panel == null)
            _panel = GetComponentInParent<PianoPanel>();

        NormalizeNote();
        ApplyIdleColor();
    }

    public void SetMarked(bool marked)
    {
        // Marked-state highlighting is intentionally disabled for this puzzle.
        ApplyIdleColor();
    }

    public bool TryPressFromInteraction()
    {
        Press();
        return true;
    }

    public void Press()
    {
        CancelInvoke(nameof(ResetAfterFlash));
        SetColor(activeColor);
        Invoke(nameof(ResetAfterFlash), Mathf.Max(0.03f, flashDuration));

        EnsureAudioSource();
        if (audioSource != null)
        {
            if (noteClip != null)
                audioSource.PlayOneShot(noteClip);
            else if (TryGetGeneratedToneClip(out var generated))
                audioSource.PlayOneShot(generated, generatedToneVolume);
        }

        _panel?.TryInteractWithKey(this);
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && Application.isPlaying)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 12f;
        }
    }

    private bool TryGetGeneratedToneClip(out AudioClip clip)
    {
        clip = null;
        if (!Application.isPlaying || string.IsNullOrEmpty(_normalizedNoteId))
            return false;

        if (GeneratedToneCache.TryGetValue(_normalizedNoteId, out clip) && clip != null)
            return true;

        if (!TryNoteToMidi(_normalizedNoteId, out int midiNote))
            return false;

        float frequency = 440f * Mathf.Pow(2f, (midiNote - 69) / 12f);
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(GeneratedToneSampleRate * generatedToneDuration));
        float[] samples = new float[sampleCount];

        float fadeInSamples = Mathf.Max(1f, sampleCount * 0.06f);
        float fadeOutStart = sampleCount * 0.82f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)GeneratedToneSampleRate;
            float envelope = 1f;
            if (i < fadeInSamples)
                envelope = i / fadeInSamples;
            else if (i > fadeOutStart)
                envelope = Mathf.Clamp01((sampleCount - i) / (sampleCount - fadeOutStart));

            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        clip = AudioClip.Create($"Tone_{_normalizedNoteId}", sampleCount, 1, GeneratedToneSampleRate, false);
        clip.SetData(samples, 0);
        GeneratedToneCache[_normalizedNoteId] = clip;
        return true;
    }

    private static bool TryNoteToMidi(string normalizedNote, out int midiNote)
    {
        midiNote = 0;
        if (string.IsNullOrEmpty(normalizedNote))
            return false;

        int semitone;
        switch (normalizedNote[0])
        {
            case 'C': semitone = 0; break;
            case 'D': semitone = 2; break;
            case 'E': semitone = 4; break;
            case 'F': semitone = 5; break;
            case 'G': semitone = 7; break;
            case 'A': semitone = 9; break;
            case 'B': semitone = 11; break;
            default: return false;
        }

        int index = 1;
        if (index < normalizedNote.Length && normalizedNote[index] == '#')
        {
            semitone += 1;
            index++;
        }

        if (index >= normalizedNote.Length || !int.TryParse(normalizedNote.Substring(index), out int octave))
            return false;

        midiNote = (octave + 1) * 12 + semitone;
        return true;
    }

    private void ResetAfterFlash()
    {
        ApplyIdleColor();
    }

    private void ApplyIdleColor()
    {
        SetColor(normalColor);
    }

    private void SetColor(Color color)
    {
        EnsureRenderer();
        if (targetRenderer == null)
            return;

        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(ColorPropertyId, color);
        _propertyBlock.SetColor(BaseColorPropertyId, color);
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void EnsureRenderer()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void NormalizeNote()
    {
        if (PianoPanel.TryNormalizeNoteToken(noteId, out string normalized))
        {
            _normalizedNoteId = normalized;
            return;
        }

        _normalizedNoteId = string.Empty;
        Debug.LogWarning($"[PianoKey] Invalid note '{noteId}' on '{name}'. Use format like C4, D#4, Bb4.", this);
    }

    private void OnMouseDown()
    {
        if (!allowMouseClick)
            return;

        Press();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        NormalizeNote();
        EnsureRenderer();
    }
#endif
}
