using UnityEngine;

public class CollectionFragmentSceneActivator : MonoBehaviour
{
    public enum ActivationMode
    {
        Persistent,
        Range
    }

    [System.Serializable]
    public class Rule
    {
        public ActivationMode activationMode = ActivationMode.Persistent;
        [Min(0)] public int minCollectedFragments;
        public int maxCollectedFragments = -1;
        public GameObject[] enableObjects;
        public GameObject[] disableObjects;
    }

    [SerializeField] private CollectionSetData collectionSet;
    [SerializeField] private Rule[] rules;
    [SerializeField] private bool refreshOnInventoryChanged = true;

    private PlayerInventory inventory;
    private bool subscribed;

    private void OnEnable()
    {
        ResolveInventory();
        SubscribeIfNeeded();
        Refresh();
    }

    private void Start()
    {
        ResolveInventory();
        SubscribeIfNeeded();
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null && subscribed)
            inventory.Changed -= Refresh;

        subscribed = false;
    }

    public void Refresh()
    {
        ResolveInventory();

        int collectedCount = GetCollectedFragmentCount();
        if (rules == null)
            return;

        for (int i = 0; i < rules.Length; i++)
            ApplyRule(rules[i], collectedCount);
    }

    private int GetCollectedFragmentCount()
    {
        if (inventory == null || collectionSet == null)
            return 0;

        bool[] states = inventory.GetCollectionPieceStates(collectionSet);
        int count = 0;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i])
                count++;
        }

        return count;
    }

    private static void ApplyRule(Rule rule, int collectedCount)
    {
        if (rule == null)
            return;

        bool isMatched = rule.activationMode == ActivationMode.Range
            ? collectedCount >= rule.minCollectedFragments && (rule.maxCollectedFragments < rule.minCollectedFragments || collectedCount <= rule.maxCollectedFragments)
            : collectedCount >= rule.minCollectedFragments;

        SetObjectsActive(rule.enableObjects, isMatched);
        SetObjectsActive(rule.disableObjects, !isMatched);
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

    private void ResolveInventory()
    {
        if (inventory != null)
            return;

        if (GameManager.inventory != null)
        {
            inventory = GameManager.inventory;
            return;
        }

        inventory = FindObjectOfType<PlayerInventory>();
    }

    private void SubscribeIfNeeded()
    {
        if (!refreshOnInventoryChanged || inventory == null || subscribed)
            return;

        inventory.Changed += Refresh;
        subscribed = true;
    }
}

