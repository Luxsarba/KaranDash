using UnityEngine;
using UnityEngine.Events;

public class ToggleObjectsOnSuccess : MonoBehaviour
{
    [SerializeField] private GameObject[] enableObjects;
    [SerializeField] private GameObject[] disableObjects;
    [SerializeField] private bool executeOnlyOnce = true;
    [SerializeField] private UnityEvent afterToggle = new UnityEvent();

    private bool hasExecuted;

    public void Execute()
    {
        if (executeOnlyOnce && hasExecuted)
            return;

        SetObjectsActive(enableObjects, true);
        SetObjectsActive(disableObjects, false);
        hasExecuted = true;
        afterToggle?.Invoke();
    }

    public void Run()
    {
        Execute();
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
