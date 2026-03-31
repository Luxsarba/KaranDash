using System;
using System.Collections.Generic;

public static class QuestProgressState
{
    private struct RuntimeState
    {
        public bool QuestGiven;
        public bool QuestCompleted;
    }

    private static readonly Dictionary<string, RuntimeState> States = new Dictionary<string, RuntimeState>();

    public static event Action Changed;

    public static void Initialize(IEnumerable<QuestProgressSaveEntry> entries)
    {
        States.Clear();

        if (entries != null)
        {
            foreach (QuestProgressSaveEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.questId))
                    continue;

                bool questCompleted = entry.questCompleted;
                bool questGiven = entry.questGiven || questCompleted;

                States[entry.questId] = new RuntimeState
                {
                    QuestGiven = questGiven,
                    QuestCompleted = questCompleted
                };
            }
        }

        Changed?.Invoke();
    }

    public static bool TryGetState(string questId, out bool questGiven, out bool questCompleted)
    {
        if (!string.IsNullOrWhiteSpace(questId) && States.TryGetValue(questId, out RuntimeState state))
        {
            questGiven = state.QuestGiven;
            questCompleted = state.QuestCompleted;
            return true;
        }

        questGiven = false;
        questCompleted = false;
        return false;
    }

    public static bool SetState(string questId, bool questGiven, bool questCompleted)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        questGiven |= questCompleted;

        if (!questGiven && !questCompleted)
        {
            if (!States.Remove(questId))
                return false;

            Changed?.Invoke();
            return true;
        }

        RuntimeState newState = new RuntimeState
        {
            QuestGiven = questGiven,
            QuestCompleted = questCompleted
        };

        if (States.TryGetValue(questId, out RuntimeState existingState) &&
            existingState.QuestGiven == newState.QuestGiven &&
            existingState.QuestCompleted == newState.QuestCompleted)
        {
            return false;
        }

        States[questId] = newState;
        Changed?.Invoke();
        return true;
    }

    public static QuestProgressSaveEntry[] GetAllEntries()
    {
        QuestProgressSaveEntry[] entries = new QuestProgressSaveEntry[States.Count];
        int index = 0;

        foreach (KeyValuePair<string, RuntimeState> pair in States)
        {
            entries[index++] = new QuestProgressSaveEntry
            {
                questId = pair.Key,
                questGiven = pair.Value.QuestGiven,
                questCompleted = pair.Value.QuestCompleted
            };
        }

        return entries;
    }

    public static void Clear()
    {
        Initialize(Array.Empty<QuestProgressSaveEntry>());
    }
}
