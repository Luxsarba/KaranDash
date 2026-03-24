using UnityEngine;

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
}
