using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target; 
    public float distance = 5.0f; 
    public Vector3 offset = new Vector3(0, 1.0f, 0); 
    public float collisionOffset = 0.2f;
    public float rayStartOffset = 0.5f; 

    void LateUpdate()
    {
        Vector3 cameraPosition = target.position - target.forward * distance + offset;
        Vector3 directionToTarget = (target.position + offset - cameraPosition).normalized;
        Vector3 raycastStart = cameraPosition + directionToTarget * rayStartOffset;

        RaycastHit hit;
        if (Physics.Raycast(raycastStart, directionToTarget, out hit, distance + rayStartOffset))
        {
            Vector3 normalOffset = hit.normal * collisionOffset;
            transform.position = hit.point + normalOffset;
        }
        else
        {
            transform.position = cameraPosition;
        }

        transform.LookAt(target.position + offset);
    }

    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.red;
            Vector3 cameraPosition = target.position - target.forward * distance + offset;
            Vector3 directionToTarget = (target.position + offset - cameraPosition).normalized;
            Vector3 raycastStart = cameraPosition + directionToTarget * rayStartOffset;
            Gizmos.DrawLine(raycastStart, raycastStart + directionToTarget * (distance + rayStartOffset));
        }
    }
}
