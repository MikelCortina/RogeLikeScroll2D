using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text quantityText;

    public void Setup(Sprite icon, string name, int quantity)
    {
        iconImage.sprite = icon;
        nameText.text = name;
        quantityText.text = "x" + quantity;
    }
}
