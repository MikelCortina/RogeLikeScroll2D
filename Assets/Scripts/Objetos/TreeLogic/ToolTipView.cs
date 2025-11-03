using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class TooltipView : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    [Header("Optional")]
    public CanvasGroup canvasGroup;

    public void SetData(ItemNode node)
    {
        if (iconImage != null) iconImage.sprite = node?.icon;
        if (titleText != null) titleText.text = node != null ? (string.IsNullOrEmpty(node.displayName) ? node.nodeId : node.displayName) : "";
        if (descriptionText != null) descriptionText.text = node != null ? node.description ?? "" : "";
        if (costText != null) costText.text = (node != null && node.cost > 0) ? $"Cost: {node.cost}" : "";
    }
}
