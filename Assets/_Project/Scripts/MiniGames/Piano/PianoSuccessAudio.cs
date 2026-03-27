using UnityEngine;

/// <summary>
/// Dedicated success-audio handler for PianoPanel.
/// Keeps audio playback logic outside of puzzle logic.
/// </summary>
public class PianoSuccessAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PianoPanel pianoPanel;
    [SerializeField] private AudioSource audioSource;

    [Header("Success Audio")]
    [SerializeField] private AudioClip successClip;
    [SerializeField, Range(0f, 1f)] private float successVolume = 1f;

    private void Awake()
    {
        ResolveReferences();
        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureAudioSource();

        if (pianoPanel != null)
            pianoPanel.Success += Play;
    }

    private void OnDisable()
    {
        if (pianoPanel != null)
            pianoPanel.Success -= Play;
    }

    /// <summary>
    /// Play success clip. Can be called from code or UnityEvent.
    /// </summary>
    public void Play()
    {
        if (successClip == null)
            return;

        ResolveReferences();
        ConfigureAudioSource();
        if (audioSource != null)
            audioSource.PlayOneShot(successClip, successVolume);
    }

    private void ResolveReferences()
    {
        if (pianoPanel == null)
            pianoPanel = GetComponent<PianoPanel>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null && Application.isPlaying)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 24f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
        ConfigureAudioSource();
        successVolume = Mathf.Clamp01(successVolume);
    }
#endif
}
