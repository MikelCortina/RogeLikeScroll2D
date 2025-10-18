using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class StatLineUI : MonoBehaviour
{
    [Header("UI refs")]
    public Image icon;
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;

    /// <summary>
    /// Inicializa la línea con icono, label, valor y estilos.
    /// </summary>
    public void Setup(Sprite iconSprite, string label, string value,
                      Color? labelColor = null, Color? valueColor = null,
                      int? labelSize = null, int? valueSize = null, bool showIcon = true)
    {
        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.gameObject.SetActive(showIcon && iconSprite != null);
        }

        if (labelText != null)
        {
            labelText.text = label;
            if (labelColor.HasValue) labelText.color = labelColor.Value;
            if (labelSize.HasValue) labelText.fontSize = labelSize.Value;
        }

        if (valueText != null)
        {
            valueText.text = value;
            if (valueColor.HasValue) valueText.color = valueColor.Value;
            if (valueSize.HasValue) valueText.fontSize = valueSize.Value;
        }
    }

    public void UpdateValue(string newValue)
    {
        if (valueText != null) valueText.text = newValue;
    }
}
