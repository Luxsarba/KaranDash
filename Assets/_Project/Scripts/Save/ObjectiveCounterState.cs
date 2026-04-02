using System;
using System.Collections.Generic;

public static class ObjectiveCounterState
{
    private static readonly Dictionary<string, int> Counters = new Dictionary<string, int>();

    public static event Action Changed;

    public static void Initialize(IEnumerable<ObjectiveCounterSaveEntry> entries)
    {
        Counters.Clear();

        if (entries != null)
        {
            foreach (ObjectiveCounterSaveEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.objectiveId))
                    continue;

                Counters[entry.objectiveId] = Math.Max(0, entry.value);
            }
        }

        Changed?.Invoke();
    }

    public static int GetValue(string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
            return 0;

        return Counters.TryGetValue(objectiveId, out int value) ? value : 0;
    }

    public static bool SetValue(string objectiveId, int value)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
            return false;

        int normalizedValue = Math.Max(0, value);

        if (normalizedValue == 0)
        {
            if (!Counters.Remove(objectiveId))
                return false;

            Changed?.Invoke();
            return true;
        }

        if (Counters.TryGetValue(objectiveId, out int existingValue) && existingValue == normalizedValue)
            return false;

        Counters[objectiveId] = normalizedValue;
        Changed?.Invoke();
        return true;
    }

    public static int Increment(string objectiveId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(objectiveId) || amount <= 0)
            return GetValue(objectiveId);

        int newValue = GetValue(objectiveId) + amount;
        SetValue(objectiveId, newValue);
        return newValue;
    }

    public static ObjectiveCounterSaveEntry[] GetAllEntries()
    {
        ObjectiveCounterSaveEntry[] entries = new ObjectiveCounterSaveEntry[Counters.Count];
        int index = 0;

        foreach (KeyValuePair<string, int> pair in Counters)
        {
            entries[index++] = new ObjectiveCounterSaveEntry
            {
                objectiveId = pair.Key,
                value = pair.Value
            };
        }

        return entries;
    }

    public static void Clear()
    {
        Initialize(Array.Empty<ObjectiveCounterSaveEntry>());
    }
}
