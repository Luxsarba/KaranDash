using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.0f;
    public Vector3 offset = new Vector3(0, 1.0f, 0);
    public float collisionOffset = 0.2f;
    public float rayStartOffset = 0.5f;
    public LayerMask collisionMask = ~0;
    [Min(0.01f)] public float cameraRadius = 0.2f;
    [Min(0f)] public float minDistance = 0.5f;
    [Min(0f)] public float distanceSmoothSpeed = 14f;
    public bool ignoreTriggerColliders = true;

    private float currentDistance;

    private void Awake()
    {
        currentDistance = Mathf.Max(minDistance, distance);
    }

    private void LateUpdate()
    {
        if (!target)
            return;

        Vector3 pivot = target.position + offset;
        Vector3 desiredDirection = -target.forward;
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            desiredDirection = -transform.forward;
        }

        desiredDirection.Normalize();

        float desiredDistance = Mathf.Max(minDistance, distance);
        float resolvedDistance = ResolveDistance(pivot, desiredDirection, desiredDistance);

        float lerpT = 1f - Mathf.Exp(-distanceSmoothSpeed * Time.deltaTime);
        currentDistance = Mathf.Lerp(currentDistance, resolvedDistance, lerpT);

        Vector3 finalPosition = pivot + desiredDirection * currentDistance;
        transform.position = finalPosition;
        transform.rotation = Quaternion.LookRotation((pivot - finalPosition).normalized, Vector3.up);
    }

    private float ResolveDistance(Vector3 pivot, Vector3 direction, float desiredDistance)
    {
        float castDistance = desiredDistance + rayStartOffset;
        QueryTriggerInteraction triggerMode = ignoreTriggerColliders
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        RaycastHit[] hits = Physics.SphereCastAll(
            pivot,
            cameraRadius,
            direction,
            castDistance,
            collisionMask,
            triggerMode
        );

        if (hits == null || hits.Length == 0)
            return desiredDistance;

        float nearestDistance = float.PositiveInfinity;
        Transform ignoredRoot = target.root;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (!col)
                continue;

            if (ignoredRoot && col.transform.IsChildOf(ignoredRoot))
                continue;

            if (hits[i].distance < nearestDistance)
                nearestDistance = hits[i].distance;
        }

        if (float.IsPositiveInfinity(nearestDistance))
            return desiredDistance;

        float safeDistance = Mathf.Max(minDistance, nearestDistance - collisionOffset);
        return Mathf.Min(desiredDistance, safeDistance);
    }

    private void OnDrawGizmos()
    {
        if (!target)
            return;

        Vector3 pivot = target.position + offset;
        Vector3 direction = -target.forward.normalized;
        float desiredDistance = Mathf.Max(minDistance, distance);
        Vector3 desiredPos = pivot + direction * desiredDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(pivot, desiredPos);
        Gizmos.DrawWireSphere(desiredPos, cameraRadius);
    }
}
