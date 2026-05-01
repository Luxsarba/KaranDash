using UnityEngine;

public class BobbingMotion : MonoBehaviour
{
    [SerializeField] private Vector3 motionAxis = Vector3.up;
    [SerializeField] private float amplitude = 0.75f;
    [SerializeField] private float frequency = 1.5f;
    [SerializeField] private bool useUnscaledTime;

    private Vector3 initialLocalPosition;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        initialLocalPosition = transform.localPosition;
        UpdatePosition();
    }

    private void Update()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        Vector3 axis = motionAxis.sqrMagnitude > 0.0001f ? motionAxis.normalized : Vector3.up;
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float offset = Mathf.Sin(time * frequency) * amplitude;
        transform.localPosition = initialLocalPosition + axis * offset;
    }
}
