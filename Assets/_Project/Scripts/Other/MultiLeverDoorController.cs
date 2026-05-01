using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MultiLeverDoorController : MonoBehaviour
{
    [Header("Levers")]
    [SerializeField] private LeverSwitch[] levers;
    [SerializeField] private GameObject[] leverObjects;

    [Header("Door Rotation")]
    [SerializeField] private Transform doorToRotate;
    [SerializeField] private Vector3 doorRotationDelta = new Vector3(0f, 90f, 0f);
    [SerializeField] private Transform secondDoorToRotate;
    [SerializeField] private Vector3 secondDoorRotationDelta = new Vector3(0f, -90f, 0f);
    [SerializeField] private bool useLocalRotation = true;
    [SerializeField, Min(0.01f)] private float rotationDuration = 0.8f;

    [Header("Object Toggle")]
    [SerializeField] private GameObject[] enableOnUnlocked;
    [SerializeField] private GameObject[] disableOnUnlocked;

    [Header("Events")]
    [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

    private bool unlocked;
    private bool isAnimating;

    private void OnEnable()
    {
        Subscribe();
        CheckUnlock();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void CheckUnlock()
    {
        if (unlocked || isAnimating || !AreAllLeversActivated())
            return;

        unlocked = true;
        StartCoroutine(UnlockRoutine());
    }

private IEnumerator UnlockRoutine()
    {
        isAnimating = true;

        SetObjectsActive(enableOnUnlocked, true);
        SetObjectsActive(disableOnUnlocked, false);

        if (doorToRotate != null || secondDoorToRotate != null)
            yield return RotateDoorsRoutine();

        onUnlocked?.Invoke();
        isAnimating = false;
    }

private IEnumerator RotateDoorsRoutine()
    {
        Quaternion firstStart = doorToRotate != null ? (useLocalRotation ? doorToRotate.localRotation : doorToRotate.rotation) : Quaternion.identity;
        Quaternion firstEnd = doorToRotate != null
            ? (useLocalRotation ? firstStart * Quaternion.Euler(doorRotationDelta) : Quaternion.Euler(doorRotationDelta) * firstStart)
            : Quaternion.identity;

        Quaternion secondStart = secondDoorToRotate != null ? (useLocalRotation ? secondDoorToRotate.localRotation : secondDoorToRotate.rotation) : Quaternion.identity;
        Quaternion secondEnd = secondDoorToRotate != null
            ? (useLocalRotation ? secondStart * Quaternion.Euler(secondDoorRotationDelta) : Quaternion.Euler(secondDoorRotationDelta) * secondStart)
            : Quaternion.identity;

        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rotationDuration));

            if (doorToRotate != null)
            {
                Quaternion currentFirst = Quaternion.Slerp(firstStart, firstEnd, t);
                if (useLocalRotation)
                    doorToRotate.localRotation = currentFirst;
                else
                    doorToRotate.rotation = currentFirst;
            }

            if (secondDoorToRotate != null)
            {
                Quaternion currentSecond = Quaternion.Slerp(secondStart, secondEnd, t);
                if (useLocalRotation)
                    secondDoorToRotate.localRotation = currentSecond;
                else
                    secondDoorToRotate.rotation = currentSecond;
            }

            yield return null;
        }

        if (doorToRotate != null)
        {
            if (useLocalRotation)
                doorToRotate.localRotation = firstEnd;
            else
                doorToRotate.rotation = firstEnd;
        }

        if (secondDoorToRotate != null)
        {
            if (useLocalRotation)
                secondDoorToRotate.localRotation = secondEnd;
            else
                secondDoorToRotate.rotation = secondEnd;
        }
    }

private bool AreAllLeversActivated()
    {
        int leverCount = GetLeverCount();
        if (leverCount == 0)
            return false;

        for (int i = 0; i < leverCount; i++)
        {
            LeverSwitch lever = GetLeverAt(i);
            if (lever == null || !lever.IsActivated)
                return false;
        }

        return true;
    }

private void Subscribe()
    {
        int leverCount = GetLeverCount();
        for (int i = 0; i < leverCount; i++)
        {
            LeverSwitch lever = GetLeverAt(i);
            if (lever != null)
                lever.Activated += HandleLeverActivated;
        }
    }

private void Unsubscribe()
    {
        int leverCount = GetLeverCount();
        for (int i = 0; i < leverCount; i++)
        {
            LeverSwitch lever = GetLeverAt(i);
            if (lever != null)
                lever.Activated -= HandleLeverActivated;
        }
    }

    

    private int GetLeverCount()
    {
        if (leverObjects != null && leverObjects.Length > 0)
            return leverObjects.Length;

        return levers != null ? levers.Length : 0;
    }

    private LeverSwitch GetLeverAt(int index)
    {
        if (leverObjects != null && index >= 0 && index < leverObjects.Length)
        {
            GameObject leverObject = leverObjects[index];
            if (leverObject != null)
                return leverObject.GetComponent<LeverSwitch>();
        }

        if (levers != null && index >= 0 && index < levers.Length)
            return levers[index];

        return null;
    }
private void HandleLeverActivated(LeverSwitch lever)
    {
        CheckUnlock();
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
