using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSettingsController : MonoBehaviour
{
    private const string SoundVolumeKey = "settings.sound_volume";
    private const string MusicVolumeKey = "settings.music_volume";
    private const string BrightnessKey = "settings.brightness";

    [Header("UI")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text soundValueText;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private TMP_Text brightnessValueText;
    [SerializeField] private Image brightnessOverlay;

    [Header("Audio")]
    [SerializeField] private AudioSource[] musicSources;

    [Header("Brightness")]
    [SerializeField, Range(0f, 1f)] private float maxOverlayAlpha = 0.65f;

    private bool _isApplyingValues;

    private void Awake()
    {
        ResolveReferences();
        ConfigureSliders();
        LoadAndApply();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe(true);
        ApplyCurrentValues();
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ResolveReferences();
    }
#endif

    public void SetSoundVolume(float value)
    {
        if (_isApplyingValues)
            return;

        value = Mathf.Clamp01(value);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(SoundVolumeKey, value);
        UpdateValueLabel(soundValueText, value);
    }

    public void SetMusicVolume(float value)
    {
        if (_isApplyingValues)
            return;

        value = Mathf.Clamp01(value);
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        UpdateValueLabel(musicValueText, value);
    }

    public void SetBrightness(float value)
    {
        if (_isApplyingValues)
            return;

        value = Mathf.Clamp01(value);
        ApplyBrightness(value);
        PlayerPrefs.SetFloat(BrightnessKey, value);
        UpdateValueLabel(brightnessValueText, value);
    }

    private void LoadAndApply()
    {
        float sound = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float brightness = PlayerPrefs.GetFloat(BrightnessKey, 1f);

        _isApplyingValues = true;

        if (soundSlider != null)
            soundSlider.SetValueWithoutNotify(sound);
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(music);
        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(brightness);

        AudioListener.volume = sound;
        ApplyMusicVolume(music);
        ApplyBrightness(brightness);
        UpdateValueLabel(soundValueText, sound);
        UpdateValueLabel(musicValueText, music);
        UpdateValueLabel(brightnessValueText, brightness);

        _isApplyingValues = false;
    }

    private void ApplyCurrentValues()
    {
        float sound = soundSlider != null ? soundSlider.value : PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
        float music = musicSlider != null ? musicSlider.value : PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        float brightness = brightnessSlider != null ? brightnessSlider.value : PlayerPrefs.GetFloat(BrightnessKey, 1f);

        AudioListener.volume = Mathf.Clamp01(sound);
        ApplyMusicVolume(music);
        ApplyBrightness(brightness);
        UpdateValueLabel(soundValueText, sound);
        UpdateValueLabel(musicValueText, music);
        UpdateValueLabel(brightnessValueText, brightness);
    }

    private void ApplyMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (musicSources == null)
            return;

        for (int i = 0; i < musicSources.Length; i++)
        {
            AudioSource source = musicSources[i];
            if (source != null)
                source.volume = value;
        }
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessOverlay == null)
            return;

        Color color = brightnessOverlay.color;
        color.a = Mathf.Lerp(maxOverlayAlpha, 0f, Mathf.Clamp01(value));
        brightnessOverlay.color = color;
    }

    private void ConfigureSliders()
    {
        ConfigureSlider(soundSlider);
        ConfigureSlider(musicSlider);
        ConfigureSlider(brightnessSlider);
    }

    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void Subscribe(bool subscribe)
    {
        BindSlider(soundSlider, SetSoundVolume, subscribe);
        BindSlider(musicSlider, SetMusicVolume, subscribe);
        BindSlider(brightnessSlider, SetBrightness, subscribe);
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback, bool subscribe)
    {
        if (slider == null || callback == null)
            return;

        if (subscribe)
            slider.onValueChanged.AddListener(callback);
        else
            slider.onValueChanged.RemoveListener(callback);
    }

    private void ResolveReferences()
    {
        if (settingsPanel == null)
            settingsPanel = FindChildGameObjectByName(transform, "Настройки");

        if (settingsPanel == null)
            return;

        if (soundSlider == null)
            soundSlider = FindSlider(settingsPanel.transform, "SoundSlider");
        if (musicSlider == null)
            musicSlider = FindSlider(settingsPanel.transform, "MusicSlider");
        if (brightnessSlider == null)
            brightnessSlider = FindSlider(settingsPanel.transform, "BrightnessSlider");

        if (soundValueText == null)
            soundValueText = FindText(settingsPanel.transform, "SoundValue");
        if (musicValueText == null)
            musicValueText = FindText(settingsPanel.transform, "MusicValue");
        if (brightnessValueText == null)
            brightnessValueText = FindText(settingsPanel.transform, "BrightnessValue");

        if (brightnessOverlay == null)
        {
            GameObject canvas = FindChildGameObjectByName(null, "Canvas");
            if (canvas != null)
            {
                Transform overlayTransform = FindChildTransformByName(canvas.transform, "BrightnessOverlay");
                if (overlayTransform != null)
                    brightnessOverlay = overlayTransform.GetComponent<Image>();
            }
        }
    }

    private static Slider FindSlider(Transform root, string name)
    {
        Transform target = FindChildTransformByName(root, name);
        return target != null ? target.GetComponent<Slider>() : null;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        Transform target = FindChildTransformByName(root, name);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static void UpdateValueLabel(TMP_Text label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private static Transform FindChildTransformByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == name)
                return children[i];
        }

        return null;
    }

    private static GameObject FindChildGameObjectByName(Transform root, string name)
    {
        if (root == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i] != null && allObjects[i].name == name)
                    return allObjects[i];
            }

            return null;
        }

        Transform child = FindChildTransformByName(root, name);
        return child != null ? child.gameObject : null;
    }
}
