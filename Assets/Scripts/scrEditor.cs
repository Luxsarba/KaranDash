using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(scr))]
public class scrEditor : Editor
{
    static float pauseAfterLoop = 0.7f;
static bool _paused;
static double _pauseStartTime;
    static bool _playing;
    static GameObject _go;
    static AnimationClip _clip;
    static double _startTime;
    static saturation _receiver;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUI.enabled = !_playing;
        if (GUILayout.Button("Start anim"))
            StartAnim();
        GUI.enabled = _playing;
        if (GUILayout.Button("Stop anim"))
            StopAnim();
        GUI.enabled = true;
    }

    void StartAnim()
    {
        var t = (scr)target;
        _go = t.gameObject;

        var anim = _go.GetComponent<Animation>();
        if (!anim || !anim.clip)
        {
            Debug.LogWarning("$No Animation component or no default clip");
            return;
        }

        _clip = anim.clip;

        _receiver = _go.GetComponentInChildren<saturation>(true);
        if (!_receiver)
        {
            Debug.LogWarning("No saturation found in children");
            return;
        }

        if (!_clip)
        {
            Debug.LogWarning($"Animation has no default clip on {_go.name}");
            return;
        }


        if (!_clip.legacy)
        {
            Debug.LogWarning($"Clip '{_clip.name}' is NOT Legacy. Set it to Legacy (AnimationClip import settings) or use Animator-version.");
            return;
        }

        if (_playing) StopAnim();

        _startTime = EditorApplication.timeSinceStartup;
        _playing = true;

        AnimationMode.StartAnimationMode();
        EditorApplication.update += OnEditorUpdate;


        SampleAt(0f);


    }

    void StopAnim()
    {
        if (!_playing) return;

        _playing = false;
        EditorApplication.update -= OnEditorUpdate;

        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();

        SceneView.RepaintAll();

    }

    void OnDisable()
    {

        StopAnim();
    }

    static void OnEditorUpdate()
    {
        if (!_playing || !_go || !_clip) return;

        double now = EditorApplication.timeSinceStartup;
        float elapsed = (float)(now - _startTime);

        
        if (_paused)
        {
    
            SampleAt(_clip.length);
            if (_receiver) _receiver.SetValue(1f);

            if (now - _pauseStartTime >= pauseAfterLoop)
            {
    
                _paused = false;
                _startTime = now;
            }
            return;
        }


        if (elapsed >= _clip.length)
        {

            _paused = true;
            _pauseStartTime = now;

            SampleAt(_clip.length);
            if (_receiver) _receiver.SetValue(1f);
            return;
        }


        float normalized = Mathf.Clamp01(elapsed / _clip.length);

        SampleAt(elapsed);
        if (_receiver) _receiver.SetValue(normalized);
    }

    static void SampleAt(float t)
    {
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(_go, _clip, t);
        AnimationMode.EndSampling();

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }
}
