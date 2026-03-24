using UnityEngine;

public class Crystal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // тут позже добавим подсчёт
            Destroy(gameObject);
        }
    }
}
