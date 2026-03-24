using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] healthSprites;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private bool showMaxValue;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (valueText == null)
            valueText = GetComponentInChildren<TMP_Text>(true);
    }

    public void SetHealth(int current, int max)
    {
        max = Mathf.Max(0, max);
        current = Mathf.Clamp(current, 0, max);

        if (valueText != null)
            valueText.text = showMaxValue ? $"{current}/{max}" : current.ToString();

        if (image == null || healthSprites == null || healthSprites.Length == 0)
            return;

        int steps = healthSprites.Length;
        int filled = (max == 0) ? 0 : Mathf.CeilToInt((float)current * steps / max);
        int index = Mathf.Clamp(filled - 1, 0, steps - 1);
        image.sprite = healthSprites[index];
    }

}
