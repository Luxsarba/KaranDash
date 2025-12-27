using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest Item")]
public class QuestItemData : ScriptableObject
{
    public string itemId;     // "pencil", "keycard_1"
    public string displayName;// "Карандаш"
    public Sprite icon;
}
