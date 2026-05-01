using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class Dialogue
{
    [Header("Отправитель")]
    public string name;

    [Tooltip("Email персонажа (name@mail.com)")]
    public string email;

    [Header("Получатель")]
    [Tooltip("Кому адресован диалог")]
    public string recipientName;

    [Header("Портрет")]
    public Sprite portrait;

    [Header("Текст")]
    [TextArea(3, 10)]
    public string[] sentences;

    [Header("События")]
    [Tooltip("События, которые сработают при показе конкретной фразы. Индекс начинается с 0.")]
    public DialogueSentenceEvent[] sentenceEvents;

    [Tooltip("Сработает только когда игрок дошёл до конца диалога. Escape считается отменой и это событие не вызывает.")]
    public UnityEvent onCompleted;
}

[System.Serializable]
public class DialogueSentenceEvent
{
    [Min(0)]
    public int sentenceIndex;
    public UnityEvent onReached;
}
