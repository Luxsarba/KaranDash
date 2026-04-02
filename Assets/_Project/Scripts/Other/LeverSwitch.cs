using System.Collections;
using UnityEngine;

public class LeverSwitch : MonoBehaviour, IPlayerInteractable
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation")]
    [SerializeField] private Transform targetToRotate;
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    [SerializeField] private bool useLocalRotation = true;
    [SerializeField] private float rotationAngle = 180f;
    [SerializeField] private float rotationDuration = 0.6f;
    [SerializeField] private bool singleUse = true;

    [Header("On Success")]
    [SerializeField] private RotateTwoObjectsOnSuccess onSuccess;

    private bool _isAnimating;
    private bool _hasBeenUsed;
    public bool TryInteract(PlayerInteractionContext context)
    {
        if (_isAnimating)
            return false;

        if (singleUse && _hasBeenUsed)
            return false;

        if (targetToRotate == null)
        {
            Debug.LogWarning("[LeverSwitch] targetToRotate is not assigned.", this);
            return false;
        }

        StartCoroutine(RotateRoutine());
        return true;
    }


    public void Configure(
        Transform rotationTarget,
        RotationAxis axis,
        bool shouldUseLocalRotation,
        float angle,
        float duration,
        bool useOnce,
        RotateTwoObjectsOnSuccess successAction)
    {
        targetToRotate = rotationTarget;
        rotationAxis = axis;
        useLocalRotation = shouldUseLocalRotation;
        rotationAngle = angle;
        rotationDuration = duration;
        singleUse = useOnce;
        onSuccess = successAction;
        _isAnimating = false;
        _hasBeenUsed = false;
    }

public void TryActivate()
    {
        TryInteract(default);
    }

    private IEnumerator RotateRoutine()
    {
        _isAnimating = true;

        Quaternion startRotation = useLocalRotation ? targetToRotate.localRotation : targetToRotate.rotation;
        Quaternion endRotation = GetTargetRotation(startRotation, rotationAngle);
        float duration = Mathf.Max(0.01f, rotationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);
            Quaternion currentRotation = Quaternion.Slerp(startRotation, endRotation, smoothedT);

            if (useLocalRotation)
                targetToRotate.localRotation = currentRotation;
            else
                targetToRotate.rotation = currentRotation;

            yield return null;
        }

        if (useLocalRotation)
            targetToRotate.localRotation = endRotation;
        else
            targetToRotate.rotation = endRotation;

        _hasBeenUsed = true;
        _isAnimating = false;

        if (onSuccess != null)
            onSuccess.Execute();
    }

    private Quaternion GetTargetRotation(Quaternion startRotation, float angle)
    {
        Quaternion deltaRotation = Quaternion.AngleAxis(angle, GetAxisVector());
        return useLocalRotation ? startRotation * deltaRotation : deltaRotation * startRotation;
    }

    private Vector3 GetAxisVector()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Y:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }
}
