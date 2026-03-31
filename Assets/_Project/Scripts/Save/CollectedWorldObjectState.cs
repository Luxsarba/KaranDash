using System;
using System.Collections.Generic;

public static class CollectedWorldObjectState
{
    private static readonly HashSet<string> CollectedIds = new HashSet<string>();

    public static event Action Changed;

    public static void Initialize(IEnumerable<string> ids)
    {
        CollectedIds.Clear();

        if (ids != null)
        {
            foreach (string id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    CollectedIds.Add(id);
            }
        }

        Changed?.Invoke();
    }

    public static bool MarkCollected(string persistentId)
    {
        if (string.IsNullOrWhiteSpace(persistentId))
            return false;

        if (!CollectedIds.Add(persistentId))
            return false;

        Changed?.Invoke();
        return true;
    }

    public static bool IsCollected(string persistentId)
    {
        return !string.IsNullOrWhiteSpace(persistentId) && CollectedIds.Contains(persistentId);
    }

    public static string[] GetAllIds()
    {
        var ids = new string[CollectedIds.Count];
        CollectedIds.CopyTo(ids);
        return ids;
    }

    public static void Clear()
    {
        Initialize(Array.Empty<string>());
    }
}
