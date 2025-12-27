using System.Collections;
using UnityEngine;

public class SaveStationAnimPlayer : MonoBehaviour
{
    [Header("Animation (Legacy)")]
    [SerializeField] private Animation anim;                 // Animation component
    [SerializeField] private AnimationClip clip;             // если пусто -> anim.clip
    [SerializeField] private float pauseAfterLoop = 0.7f;

    [Header("Effect Receiver")]
    [SerializeField] private saturation receiver;            // твой компонент saturation (в детях)
    [SerializeField] private bool findReceiverInChildren = true;

    private Coroutine _routine;

    private void Awake()
    {
        if (!anim) anim = GetComponent<Animation>();
        if (!clip && anim) clip = anim.clip;

        if (!receiver && findReceiverInChildren)
            receiver = GetComponentInChildren<saturation>(true);

        // Защита: если клип не legacy — Animation его не сыграет корректно
        if (clip && !clip.legacy)
            Debug.LogWarning($"[SaveStationAnimPlayer] Clip '{clip.name}' is NOT Legacy. Enable Legacy in import settings or use Animator.");
    }

    public bool IsPlaying => _routine != null;

    public void PlayOnceWithPause()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;

        if (anim && clip) anim.Stop(clip.name);
    }

    private IEnumerator PlayRoutine()
    {
        if (!anim || !clip)
        {
            Debug.LogWarning("[SaveStationAnimPlayer] No Animation or Clip.");
            _routine = null;
            yield break;
        }

        // на всякий случай добавим клип в Animation
        if (anim.GetClip(clip.name) == null)
            anim.AddClip(clip, clip.name);

        // старт
        anim.Play(clip.name);

        float t = 0f;
        float len = Mathf.Max(0.0001f, clip.length);

        // В рантайме Animation сам проигрывает клип.
        // Мы только синхронизируем saturation по времени.
        while (t < len)
        {
            float normalized = Mathf.Clamp01(t / len);
            if (receiver) receiver.SetValue(normalized);

            t += Time.unscaledDeltaTime; // важно: чтобы работало даже если Time.timeScale = 0
            yield return null;
        }

        // зафиксировать конец
        if (receiver) receiver.SetValue(1f);

        // пауза после проигрыша
        float p = 0f;
        while (p < pauseAfterLoop)
        {
            p += Time.unscaledDeltaTime;
            yield return null;
        }

        _routine = null;
    }
}
