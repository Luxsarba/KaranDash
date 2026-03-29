using System.Collections;
using UnityEngine;

public class RotateTwoObjectsOnSuccess : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform firstTarget;
    [SerializeField] private Vector3 firstRotationDelta;
    [SerializeField] private Transform secondTarget;
    [SerializeField] private Vector3 secondRotationDelta;

    [Header("Animation")]
    [SerializeField] private bool useLocalRotation = true;
    [SerializeField] private float rotationDuration = 0.8f;
    [SerializeField] private bool executeOnlyOnce = true;

    private bool _isAnimating;
    private bool _hasExecuted;

    public void Configure(
        Transform firstRotationTarget,
        Vector3 firstDelta,
        Transform secondRotationTarget,
        Vector3 secondDelta,
        bool shouldUseLocalRotation,
        float duration,
        bool onlyOnce)
    {
        firstTarget = firstRotationTarget;
        firstRotationDelta = firstDelta;
        secondTarget = secondRotationTarget;
        secondRotationDelta = secondDelta;
        useLocalRotation = shouldUseLocalRotation;
        rotationDuration = duration;
        executeOnlyOnce = onlyOnce;
        _isAnimating = false;
        _hasExecuted = false;
    }

    public void Execute()
    {
        if (_isAnimating)
            return;

        if (executeOnlyOnce && _hasExecuted)
            return;

        if (firstTarget == null && secondTarget == null)
        {
            Debug.LogWarning("[RotateTwoObjectsOnSuccess] No targets assigned.", this);
            return;
        }

        StartCoroutine(RotateTargetsRoutine());
    }

    private IEnumerator RotateTargetsRoutine()
    {
        _isAnimating = true;

        Quaternion firstStart = firstTarget != null ? GetCurrentRotation(firstTarget) : Quaternion.identity;
        Quaternion secondStart = secondTarget != null ? GetCurrentRotation(secondTarget) : Quaternion.identity;
        Quaternion firstEnd = firstTarget != null ? GetTargetRotation(firstStart, firstRotationDelta) : Quaternion.identity;
        Quaternion secondEnd = secondTarget != null ? GetTargetRotation(secondStart, secondRotationDelta) : Quaternion.identity;
        float duration = Mathf.Max(0.01f, rotationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            if (firstTarget != null)
                SetRotation(firstTarget, Quaternion.Slerp(firstStart, firstEnd, smoothedT));

            if (secondTarget != null)
                SetRotation(secondTarget, Quaternion.Slerp(secondStart, secondEnd, smoothedT));

            yield return null;
        }

        if (firstTarget != null)
            SetRotation(firstTarget, firstEnd);

        if (secondTarget != null)
            SetRotation(secondTarget, secondEnd);

        _hasExecuted = true;
        _isAnimating = false;
    }

    private Quaternion GetCurrentRotation(Transform target)
    {
        return useLocalRotation ? target.localRotation : target.rotation;
    }

    private void SetRotation(Transform target, Quaternion rotationValue)
    {
        if (useLocalRotation)
            target.localRotation = rotationValue;
        else
            target.rotation = rotationValue;
    }

    private Quaternion GetTargetRotation(Quaternion startRotation, Vector3 deltaEuler)
    {
        Quaternion deltaRotation = Quaternion.Euler(deltaEuler);
        return useLocalRotation ? startRotation * deltaRotation : deltaRotation * startRotation;
    }
}
