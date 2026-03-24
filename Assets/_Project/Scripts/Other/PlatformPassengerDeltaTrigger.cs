using UnityEngine;

public class PlatformPassengerDeltaTrigger : MonoBehaviour
{
    [Header("Trigger zone")]
    [SerializeField] private Collider triggerZone; // BoxCollider (isTrigger=true)
    [SerializeField] private string passengerTag = "Player";

    [Header("Options")]
    [SerializeField] private bool requireGroundedCheck = false; // если хочешь “только когда стоит”
    [SerializeField] private float groundedRay = 0.2f;          // луч вниз от игрока

    private Transform passenger;
    private Rigidbody passengerRb;

    private Vector3 lastPlatformPos;

    private void Awake()
    {
        lastPlatformPos = transform.position;

        if (!triggerZone)
            Debug.LogError("[PlatformPassengerDeltaTrigger] Assign triggerZone (BoxCollider isTrigger=true).");
        else if (!triggerZone.isTrigger)
            Debug.LogWarning("[PlatformPassengerDeltaTrigger] triggerZone должен быть isTrigger = true.");
    }

    private void FixedUpdate()
    {
        Vector3 delta = transform.position - lastPlatformPos;
        lastPlatformPos = transform.position;

        if (passengerRb == null) return;

        if (requireGroundedCheck && !IsStandingOnPlatform(passengerRb))
            return;

        passengerRb.MovePosition(passengerRb.position + delta);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(passengerTag)) return;

        passenger = other.transform;
        passengerRb = other.attachedRigidbody;

        // если вдруг у Player Rigidbody на корне, а триггер поймал child-коллайдер:
        if (passengerRb == null)
            passengerRb = other.GetComponentInParent<Rigidbody>();

        // на всякий случай сразу синхронизируем, чтобы не было прыжка
        lastPlatformPos = transform.position;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(passengerTag)) return;

        // если вышел именно наш пассажир
        if (other.attachedRigidbody == passengerRb || other.transform == passenger)
        {
            passenger = null;
            passengerRb = null;
        }
    }

    private bool IsStandingOnPlatform(Rigidbody rb)
    {
        // простой grounded-чек: луч вниз чуть-чуть, чтобы не “ехать” когда игрок прыгает в зоне
        Vector3 origin = rb.position + Vector3.up * 0.05f;
        return Physics.Raycast(origin, Vector3.down, groundedRay);
    }
}
