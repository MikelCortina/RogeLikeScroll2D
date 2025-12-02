using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryTooltipView : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI rarityText;
    public Image iconImage;

    public void SetData(IObjetos item)
    {
        if (item == null) return;

        if (titleText != null)
            titleText.text = item.name;

        if (descriptionText != null)
            descriptionText.text = item.description;

        if (rarityText != null)
            rarityText.text = item.quality.ToString();

        if (iconImage != null)
            iconImage.sprite = item.icon;
    }
}
