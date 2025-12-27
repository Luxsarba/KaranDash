using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] healthSprites;

    public void SetHealth(int current, int max)
    {
        current = Mathf.Clamp(current, 0, max);

        float t = (max == 0) ? 0f : (float)current / max;

        int steps = healthSprites.Length;
        int filled = (max == 0) ? 0 : Mathf.CeilToInt((float)current * steps / max);
        int index = Mathf.Clamp(filled - 1, 0, steps - 1);

        if (healthSprites != null && index >= 0 && index < healthSprites.Length)
            image.sprite = healthSprites[index];
    }

}
